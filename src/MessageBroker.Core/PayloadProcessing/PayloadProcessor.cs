using System;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Core.PayloadProcessing
{
    public class PayloadProcessor : IPayloadProcessor
    {
        private readonly ISerializer _serializer;
        private readonly ISendQueueStore _sendQueueStore;
        private readonly IQueueStore _queueStore;

        public PayloadProcessor(ISerializer serializer, ISendQueueStore sendQueueStore, IQueueStore queueStore)
        {
            _serializer = serializer;
            _sendQueueStore = sendQueueStore;
            _queueStore = queueStore;
        }

        public void OnDataReceived(Guid sessionId, Memory<byte> data)
        {
            var type = _serializer.ParsePayloadType(data);
            
            Logger.LogInformation($"received type {type}");

            switch (type)
            {
                case PayloadType.Msg:
                    var message = _serializer.ToMessage(data);
                    OnMessage(sessionId, message);
                    break;
                case PayloadType.Ack:
                    var ack = _serializer.ToAck(data);
                    OnMessageAck(sessionId, ack);
                    break;
                case PayloadType.Nack:
                    var nack = _serializer.ToNack(data);
                    OnMessageNack(sessionId, nack);
                    break;
                case PayloadType.SubscribeQueue:
                    var subscribeQueue = _serializer.ToSubscribeQueue(data);
                    OnSubscribeQueue(sessionId, subscribeQueue);
                    break;
                case PayloadType.UnSubscribeQueue:
                    var unsubscribeQueue = _serializer.ToUnsubscribeQueue(data);
                    OnUnsubscribeQueue(sessionId, unsubscribeQueue);
                    break;
                case PayloadType.ConfigureSubscription:
                    var subscribe = _serializer.ToConfigureSubscription(data);
                    OnConfigureSubscription(sessionId, subscribe);
                    break;
                case PayloadType.QueueCreate:
                    var queueDeclare = _serializer.ToQueueDeclareModel(data);
                    OnDeclareQueue(sessionId, queueDeclare);
                    break;
                case PayloadType.QueueDelete:
                    var queueDelete = _serializer.ToQueueDeleteModel(data);
                    OnDeleteQueue(sessionId, queueDelete);
                    break;
            }
        }


        private void OnMessage(Guid sessionId, Message message)
        {
            // dispatch the message to matched queues
            foreach (var queue in _queueStore.GetAll())
                if (queue.MessageRouteMatch(message.Route))
                    queue.OnMessage(message);

            // send received ack to publisher
            SendReceivedPayloadOk(sessionId, message.Id);

            // must return the original message data to buffer pool
            message.Dispose();
        }

        private void OnMessageAck(Guid sessionId, Ack ack)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
                sendQueue.OnMessageAckReceived(ack.Id);
        }

        private void OnMessageNack(Guid sessionId, Nack nack)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
                sendQueue.OnMessageNackReceived(nack.Id);
        }

        private void OnSubscribeQueue(Guid sessionId, SubscribeQueue subscribeQueue)
        {
            if (_queueStore.TryGetValue(subscribeQueue.QueueName, out var queue))
            {
                queue.SessionSubscribed(sessionId);
                SendReceivedPayloadOk(sessionId, subscribeQueue.Id);
            }
            else
            {
                SendReceivePayloadError(sessionId, subscribeQueue.Id, "Queue not found");
            }
        }

        private void OnUnsubscribeQueue(Guid sessionId, UnsubscribeQueue unsubscribeQueue)
        {
            if (_queueStore.TryGetValue(unsubscribeQueue.QueueName, out var queue))
            {
                queue.SessionUnSubscribed(sessionId);
                SendReceivedPayloadOk(sessionId, unsubscribeQueue.Id);
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
                    SendReceivedPayloadOk(sessionId, queueDeclare.Id);
                else
                    SendReceivePayloadError(sessionId, queueDeclare.Id, "Queue name already exists");

                return;
            }

            // create new queue
            _queueStore.Add(queueDeclare.Name, queueDeclare.Route);

            SendReceivedPayloadOk(sessionId, queueDeclare.Id);
        }

        private void OnDeleteQueue(Guid sessionId, QueueDelete queueDelete)
        {
            Logger.LogInformation($"deleting queue with name {queueDelete.Name}");

            _queueStore.Delete(queueDelete.Name);

            SendReceivedPayloadOk(sessionId, queueDelete.Id);
        }

        private void OnConfigureSubscription(Guid sessionId, ConfigureSubscription configureSubscription)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
            {
                sendQueue.Configure(configureSubscription.Concurrency, configureSubscription.AutoAck);
                SendReceivedPayloadOk(sessionId, configureSubscription.Id);
            }
        }

        private void SendReceivedPayloadOk(Guid sessionId, Guid payloadId)
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
                    Id = payloadId,
                    Message = message
                };
                var sendPayload = _serializer.Serialize(error);
                sendQueue.Enqueue(sendPayload);
            }
        }
    }
}