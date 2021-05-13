using System;
using System.Linq;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Clients.Store;
using MessageBroker.Core.Persistence.Topics;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MessageBroker.Core.PayloadProcessing
{
    /// <inheritdoc />
    public class PayloadProcessor : IPayloadProcessor
    {
        private readonly IDeserializer _deserializer;
        private readonly ISerializer _serializer;
        private readonly IClientStore _clientStore;
        private readonly ITopicStore _topicStore;
        private readonly ILogger<PayloadProcessor> _logger;

        public PayloadProcessor(IDeserializer deserializer, ISerializer serializer, IClientStore clientStore,
            ITopicStore topicStore, ILogger<PayloadProcessor> logger)
        {
            _deserializer = deserializer;
            _serializer = serializer;
            _clientStore = clientStore;
            _topicStore = topicStore;
            _logger = logger;
        }

        public void OnDataReceived(Guid sessionId, Memory<byte> data)
        {
            try
            {
                var type = _deserializer.ParsePayloadType(data);

                _logger.LogInformation($"Received data with type: {type} from client: {sessionId}");

                switch (type)
                {
                    case PayloadType.Msg:
                        var message = _deserializer.ToMessage(data);
                        OnMessage(sessionId, message);
                        break;
                    case PayloadType.Ack:
                        var ack = _deserializer.ToAck(data);
                        OnMessageAck(sessionId, ack);
                        break;
                    case PayloadType.Nack:
                        var nack = _deserializer.ToNack(data);
                        OnMessageNack(sessionId, nack);
                        break;
                    case PayloadType.SubscribeTopic:
                        var subscribeQueue = _deserializer.ToSubscribeTopic(data);
                        OnSubscribeTopic(sessionId, subscribeQueue);
                        break;
                    case PayloadType.UnsubscribeTopic:
                        var unsubscribeQueue = _deserializer.ToUnsubscribeTopic(data);
                        OnUnsubscribeTopic(sessionId, unsubscribeQueue);
                        break;
                    case PayloadType.TopicDeclare:
                        var queueDeclare = _deserializer.ToTopicDeclareModel(data);
                        OnDeclareQueue(sessionId, queueDeclare);
                        break;
                    case PayloadType.TopicDelete:
                        var queueDelete = _deserializer.ToTopicDeleteModel(data);
                        OnDeleteQueue(sessionId, queueDelete);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to deserialize data from client: {sessionId} with error: {e}");

            }
           
        }


        private void OnMessage(Guid sessionId, Message message)
        {

            if (!_topicStore.GetAll().Any())
            {
                _logger.LogInformation($"No topic was found for message with id: {message.Id}");
            }
            
            // dispatch the message to matched queues
            foreach (var topic in _topicStore.GetAll())
            {
                if (topic.MessageRouteMatch(message.Route))
                {
                    try
                    {
                        topic.OnMessage(message);
                    }
                    catch
                    {
                        _logger.LogError($"Failed to write message: {message.Id} to topic: {topic.Name}");
                        // message was not written, probably the channel was completed due to being disposed
                    }
                }
                else
                {
                    _logger.LogInformation($"No topic was found for message with id: {message.Id}");
                }
            }

            // send received ack to publisher
            SendReceivedPayloadOk(sessionId, message.Id);

            // must return the original message data to buffer pool
            message.Dispose();
        }

        private void OnMessageAck(Guid clientId, Ack ack)
        {
            _logger.LogInformation($"Ack received for message with id: {ack.Id} from client: {clientId}");

            if (_clientStore.TryGet(clientId, out var client))
                client.OnPayloadAckReceived(ack.Id);
        }

        private void OnMessageNack(Guid clientId, Nack nack)
        {
            _logger.LogInformation($"Nack received for message with id: {nack.Id} from client: {clientId}");

            if (_clientStore.TryGet(clientId, out var client))
                client.OnPayloadNackReceived(nack.Id);
        }

        private void OnSubscribeTopic(Guid clientId, SubscribeTopic subscribeTopic)
        {
            _clientStore.TryGet(clientId, out var client);

            if (client is null)
            {
                Log.Warning($"The client for id {clientId} was not found");
                SendReceivePayloadError(clientId, subscribeTopic.Id, "Internal error");
                return;
            }

            if (_topicStore.TryGetValue(subscribeTopic.TopicName, out var topic))
            {
                topic.ClientSubscribed(client);
                SendReceivedPayloadOk(clientId, subscribeTopic.Id);
            }
            else
            {
                SendReceivePayloadError(clientId, subscribeTopic.Id, "Queue not found");
            }
        }

        private void OnUnsubscribeTopic(Guid clientId, UnsubscribeTopic unsubscribeTopic)
        {
            _clientStore.TryGet(clientId, out var client);

            if (client is null)
            {
                Log.Warning($"The client for id {clientId} was not found");
                SendReceivePayloadError(clientId, unsubscribeTopic.Id, "Internal error");
                return;
            }
            
            if (_topicStore.TryGetValue(unsubscribeTopic.TopicName, out var queue))
            {
                queue.ClientUnsubscribed(client);
                SendReceivedPayloadOk(clientId, unsubscribeTopic.Id);
            }
            else
            {
                SendReceivePayloadError(clientId, unsubscribeTopic.Id, "Queue not found");
            }
        }


        private void OnDeclareQueue(Guid sessionId, TopicDeclare topicDeclare)
        {
            Logger.LogInformation($"declaring queue with name {topicDeclare.Name}");

            // if queue exists
            if (_topicStore.TryGetValue(topicDeclare.Name, out var queue))
            {
                // if queue route match
                if (queue.Route == topicDeclare.Route)
                    SendReceivedPayloadOk(sessionId, topicDeclare.Id);
                else
                    SendReceivePayloadError(sessionId, topicDeclare.Id, "Queue name already exists");

                return;
            }

            // create new queue
            _topicStore.Add(topicDeclare.Name, topicDeclare.Route);

            SendReceivedPayloadOk(sessionId, topicDeclare.Id);
        }

        private void OnDeleteQueue(Guid sessionId, TopicDelete topicDelete)
        {
            Logger.LogInformation($"deleting queue with name {topicDelete.Name}");

            _topicStore.Delete(topicDelete.Name);

            SendReceivedPayloadOk(sessionId, topicDelete.Id);
        }

        private void SendReceivedPayloadOk(Guid sessionId, Guid payloadId)
        {
            if (_clientStore.TryGet(sessionId, out var sendQueue))
            {
                var ok = new Ok
                {
                    Id = payloadId
                };
                var sendPayload = _serializer.Serialize(ok);
                sendQueue.EnqueueIgnore(sendPayload);
            }
        }

        private void SendReceivePayloadError(Guid sessionId, Guid payloadId, string message)
        {
            if (_clientStore.TryGet(sessionId, out var sendQueue))
            {
                var error = new Error
                {
                    Id = payloadId,
                    Message = message
                };
                var sendPayload = _serializer.Serialize(error);
                sendQueue.EnqueueIgnore(sendPayload);
            }
        }
    }
}