using System;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Models;
using MessageBroker.Serialization;
using MessageBroker.Socket;
using MessageBroker.Socket.Client;

namespace MessageBroker.Core
{
    public class Coordinator : ISocketEventProcessor, ISocketDataProcessor
    {
        private readonly ISendQueueStore _sendQueueStore;
        private readonly IMessageStore _messageStore;
        private readonly IQueueStore _queueStore;
        private readonly ISerializer _serializer;

        public Coordinator(ISerializer serializer, ISendQueueStore sendQueueStore, IQueueStore queueStore,
            IMessageStore messageStore)
        {
            _serializer = serializer;
            _sendQueueStore = sendQueueStore;
            _queueStore = queueStore;
            _messageStore = messageStore;
        }

        public void DataReceived(Guid sessionId, Memory<byte> payloadData)
        {
            try
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
                        var nack = _serializer.ToNack(payloadData);
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
                    case PayloadType.ConfigureSubscription:
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
            catch (Exception e)
            {
                Logger.LogError($"An error occured while trying to dispatch messages, error: {e}");
                SendReceivePayloadError(sessionId, Guid.Empty, "An error occured while trying to process payload");
            }
        }

        public void ClientConnected(IClientSession clientSession)
        {
            Logger.LogInformation("client session connected, added send queue");
            _sendQueueStore.Add(clientSession);
        }

        public void ClientDisconnected(IClientSession clientSession)
        {
            Logger.LogInformation("client session disconnected, removing send queue");

            foreach (var queue in _queueStore.GetAll())
                queue.SessionDisconnected(clientSession.Id);

            _sendQueueStore.Remove(clientSession);
        }

        public void Setup()
        {
            _queueStore.Setup();
            _messageStore.Setup();
        }

        public void OnMessage(Guid sessionId, Message message)
        {
            // dispatch the message to matched queues
            foreach (var queue in _queueStore.GetAll())
                if (queue.MessageRouteMatch(message.Route))
                    queue.OnMessage(message);

            // send received ack to publisher
            SendReceivedPayloadAck(sessionId, message.Id);

            // must return the original message data to buffer pool
            message.Dispose();
        }

        public void OnMessageAck(Guid sessionId, Ack ack)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
                sendQueue.OnMessageAckReceived(ack.Id);
        }

        public void OnMessageNack(Guid sessionId, Nack nack)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
                sendQueue.OnMessageNackReceived(nack.Id);
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
                SendReceivePayloadError(sessionId, subscribeQueue.Id, "Queue not found");
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
                SendReceivePayloadError(sessionId, unsubscribeQueue.Id, "Queue not found");
            }
        }


        private void OnDeclareQueue(Guid sessionId, QueueDeclare queueDeclare)
        {
            Logger.LogInformation($"declaring queue with name {queueDeclare.Name}");

            // if queue exists
            if (_queueStore.TryGetValue(queueDeclare.Name, out var queue))
            {
                // if queue route match
                if (queue.Route == queueDeclare.Route)
                    SendReceivedPayloadAck(sessionId, queueDeclare.Id);
                else
                    SendReceivePayloadError(sessionId, queueDeclare.Id, "Queue name already exists");

                return;
            }

            // create new queue
            _queueStore.Add(queueDeclare.Name, queueDeclare.Route);

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
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
            {
                sendQueue.Configure(configureSubscription.Concurrency, configureSubscription.AutoAck);
                SendReceivedPayloadAck(sessionId, configureSubscription.Id);
            }
        }

        private void SendReceivedPayloadAck(Guid sessionId, Guid payloadId)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
            {
                var ok = new Ok
                {
                    Id = payloadId
                };
                var sendPayload = _serializer.Serialize(ok);
                sendQueue.Enqueue(sendPayload);
            }
        }

        private void SendReceivePayloadError(Guid sessionId, Guid payloadId, string message)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
            {
                var error = new Error
                {
                    Id = payloadId
                };
                var sendPayload = _serializer.Serialize(error);
                sendQueue.Enqueue(sendPayload);
            }
        }
    }
}