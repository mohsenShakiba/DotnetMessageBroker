using System;
using System.Buffers;
using System.Collections.Generic;
using MessageBroker.Common.Logging;
using MessageBroker.Core.MessageIdTracking;
using MessageBroker.Core.Queues;
using MessageBroker.Core.StatRecording;
using MessageBroker.Models;
using MessageBroker.Serialization;
using MessageBroker.SocketServer.Abstractions;

namespace MessageBroker.Core
{
    public class Coordinator : ISocketEventProcessor
    {
        private readonly MessageDispatcher _messageDispatcher;
        private readonly IMessageIdTracker _messageIdTracker;
        private readonly IQueueStore _queueStore;
        private readonly IStatRecorder _statRecorder;
        private readonly ISerializer _serializer;

        public Coordinator(ISerializer serializer, MessageDispatcher messageDispatcher,
            IMessageIdTracker messageIdTracker, IQueueStore queueStore, IStatRecorder statRecorder)
        {
            _serializer = serializer;
            _messageDispatcher = messageDispatcher;
            _messageIdTracker = messageIdTracker;
            _queueStore = queueStore;
            _statRecorder = statRecorder;
        }

        public void ClientConnected(Guid sessionId)
        {
            Logger.LogInformation("client session connected, added send queue");
            _messageDispatcher.AddSendQueue(sessionId);
        }

        public void ClientDisconnected(Guid sessionId)
        {
            Logger.LogInformation("client session disconnected, removing send queue");
            
            foreach (var queue in _queueStore.Queues)
                queue.SessionDisconnected(sessionId);

            _messageDispatcher.RemoveSendQueue(sessionId);
        }

        public void DataReceived(Guid sessionId, Memory<byte> payloadData)
        {
            var type = _serializer.ParsePayloadType(payloadData);

            switch (type)
            {
                case PayloadType.Msg:
                    var message = _serializer.ToMessage(payloadData);
                    OnMessage(sessionId, message);
                    break;
                case PayloadType.Ack:
                    var ack = _serializer.ToAck(payloadData);
                    OnMessageAck(sessionId, ack);
                    break;
                case PayloadType.Nack:
                    var nack = _serializer.ToAck(payloadData);
                    OnMessageNack(sessionId, nack);
                    break;
                case PayloadType.SubscribeQueue:
                    var subscribeQueue = _serializer.ToSubscribeQueue(payloadData);
                    OnSubscribeQueue(sessionId, subscribeQueue);
                    break;
                case PayloadType.UnSubscribeQueue:
                    var unsubscribeQueue = _serializer.ToUnsubscribeQueue(payloadData);
                    OnUnsubscribeQueue(sessionId, unsubscribeQueue);
                    break;
                case PayloadType.Register:
                    var subscribe = _serializer.ToConfigureSubscription(payloadData);
                    OnConfigureSubscription(sessionId, subscribe);
                    break;
                case PayloadType.QueueCreate:
                    var queueDeclare = _serializer.ToQueueDeclareModel(payloadData);
                    OnDeclareQueue(sessionId, queueDeclare);
                    break;
                case PayloadType.QueueDelete:
                    var queueDelete = _serializer.ToQueueDeleteModel(payloadData);
                    OnDeleteQueue(sessionId, queueDelete);
                    break;
            }
        }

        public void OnMessage(Guid sessionId, Message message)
        {
            // dispatch the message to matched queues
            _queueStore.Dispatch(message.Route, message);

            // send received ack to publisher
            SendReceivedPayloadAck(sessionId, message.Id);

            _statRecorder.OnMessageReceived();

            // must return the original message data to buffer pool
            ArrayPool<byte>.Shared.Return(message.OriginalMessageData);
        }

        public void OnMessageAck(Guid sessionId, Ack ack)
        {
            // find the queue that is responsible for sending this message
            var queueName = _messageIdTracker.ResolveMessageId(ack.Id);

            // if the queue is found, then call OnMessageAck
            if (_queueStore.TryGetValue(queueName, out var queue))
            {
                queue.OnMessageAck(sessionId, ack.Id);
            }
        }

        public void OnMessageNack(Guid sessionId, Ack nack)
        {
            // find the queue responsible for this message
            var queueName = _messageIdTracker.ResolveMessageId(nack.Id);

            // if the queue is found, then call OnMessageNack
            if (_queueStore.TryGetValue(queueName, out var queue))
            {
                queue.OnMessageNack(sessionId, nack.Id);
            }
        }

        public void OnSubscribeQueue(Guid sessionId, SubscribeQueue subscribeQueue)
        {
            if (_queueStore.TryGetValue(subscribeQueue.QueueName, out var queue))
            {
                queue.SessionSubscribed(sessionId);
                SendReceivedPayloadAck(sessionId, subscribeQueue.Id);
            }
            else
            {
                SendReceivedPayloadNack(sessionId, subscribeQueue.Id);
            }
        }

        public void OnUnsubscribeQueue(Guid sessionId, UnsubscribeQueue unsubscribeQueue)
        {
            if (_queueStore.TryGetValue(unsubscribeQueue.QueueName, out var queue))
            {
                queue.SessionUnSubscribed(sessionId);
                SendReceivedPayloadAck(sessionId, unsubscribeQueue.Id);
            }
            else
            {
                SendReceivedPayloadNack(sessionId, unsubscribeQueue.Id);
            }
        }


        private void OnDeclareQueue(Guid sessionId, QueueDeclare queueDeclare)
        {
            Logger.LogInformation($"declaring queue with name {queueDeclare.Name}");
            
            // check if queue exists 
            var queue = _queueStore.Get(queueDeclare.Name);

            // if queue exists
            if (queue != null)
            {
                // if queue route match
                if (queue.Route == queueDeclare.Route)
                    SendReceivedPayloadAck(sessionId, queueDeclare.Id);
                else
                    SendReceivedPayloadNack(sessionId, queueDeclare.Id);

                return;
            }

            // create new queue
            _queueStore.Add(queueDeclare);

            SendReceivedPayloadAck(sessionId, queueDeclare.Id);
        }

        private void OnDeleteQueue(Guid sessionId, QueueDelete queueDelete)
        {
            Logger.LogInformation($"deleting queue with name {queueDelete.Name}");
            
            _queueStore.Remove(queueDelete.Name);

            SendReceivedPayloadAck(sessionId, queueDelete.Id);
        }

        private void OnConfigureSubscription(Guid sessionId, ConfigureSubscription configureSubscription)
        {
            _messageDispatcher.ConfigureSubscription(
                sessionId,
                configureSubscription.Concurrency,
                configureSubscription.AutoAck);

            SendReceivedPayloadAck(sessionId, configureSubscription.Id);
        }

        private void SendReceivedPayloadAck(Guid sessionId, Guid payloadId)
        {
            var ack = new Ack
            {
                Id = payloadId
            };

            var sendPayload = _serializer.ToSendPayload(ack);

            _messageDispatcher.Dispatch(sendPayload, sessionId);
        }

        private void SendReceivedPayloadNack(Guid sessionId, Guid payloadId)
        {
            var nack = new Nack
            {
                Id = payloadId
            };

            var sendPayload = _serializer.ToSendPayload(nack);

            _messageDispatcher.Dispatch(sendPayload, sessionId);
        }
    }
}