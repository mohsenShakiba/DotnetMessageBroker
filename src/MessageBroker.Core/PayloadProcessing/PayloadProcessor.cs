using System;
using System.Linq;
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

        public void OnDataReceived(Guid clientId, Memory<byte> data)
        {
            try
            {
                var type = _deserializer.ParsePayloadType(data);

                _logger.LogInformation($"Received data with type: {type} from client: {clientId}");

                switch (type)
                {
                    case PayloadType.Msg:
                        var message = _deserializer.ToMessage(data);
                        OnMessage(clientId, message);
                        break;
                    case PayloadType.Ack:
                        var ack = _deserializer.ToAck(data);
                        OnMessageAck(clientId, ack);
                        break;
                    case PayloadType.Nack:
                        var nack = _deserializer.ToNack(data);
                        OnMessageNack(clientId, nack);
                        break;
                    case PayloadType.SubscribeTopic:
                        var subscribeQueue = _deserializer.ToSubscribeTopic(data);
                        OnSubscribeTopic(clientId, subscribeQueue);
                        break;
                    case PayloadType.UnsubscribeTopic:
                        var unsubscribeQueue = _deserializer.ToUnsubscribeTopic(data);
                        OnUnsubscribeTopic(clientId, unsubscribeQueue);
                        break;
                    case PayloadType.TopicDeclare:
                        var queueDeclare = _deserializer.ToTopicDeclare(data);
                        OnDeclareQueue(clientId, queueDeclare);
                        break;
                    case PayloadType.TopicDelete:
                        var queueDelete = _deserializer.ToTopicDelete(data);
                        OnDeleteQueue(clientId, queueDelete);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to deserialize data from client: {clientId} with error: {e}");

            }
           
        }


        private void OnMessage(Guid clientId, Message message)
        {

            var matchedAnyTopic = false;
            
            // dispatch the message to matched queues
            foreach (var topic in _topicStore.GetAll())
            {
                if (topic.MessageRouteMatch(message.Route))
                {
                    try
                    {
                        topic.OnMessage(message);
                        matchedAnyTopic = true;
                    }
                    // message was not written, probably the channel was completed due to being disposed
                    catch
                    {
                        _logger.LogError($"Failed to write message: {message.Id} to topic: {topic.Name}");
                    }
                }
            }

            if (matchedAnyTopic)
            {
                // send received ack to publisher
                SendReceivedPayloadOk(clientId, message.Id);
            }
            else
            {
                SendReceivePayloadError(clientId, message.Id, "No topic was found");
            }

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


        private void OnDeclareQueue(Guid clientId, TopicDeclare topicDeclare)
        {
            _logger.LogInformation($"declaring topic: {topicDeclare.Name}");

            // if queue exists
            if (_topicStore.TryGetValue(topicDeclare.Name, out var queue))
            {
                // if queue route match
                if (queue.Route == topicDeclare.Route)
                    SendReceivedPayloadOk(clientId, topicDeclare.Id);
                else
                    SendReceivePayloadError(clientId, topicDeclare.Id, "Queue name already exists");

                return;
            }

            // create new queue
            _topicStore.Add(topicDeclare.Name, topicDeclare.Route);

            SendReceivedPayloadOk(clientId, topicDeclare.Id);
        }

        private void OnDeleteQueue(Guid clientId, TopicDelete topicDelete)
        {
            _logger.LogInformation($"Deleting topic: {topicDelete.Name}");

            _topicStore.Delete(topicDelete.Name);

            SendReceivedPayloadOk(clientId, topicDelete.Id);
        }

        private void SendReceivedPayloadOk(Guid clientId, Guid payloadId)
        {
            if (_clientStore.TryGet(clientId, out var sendQueue))
            {
                var ok = new Ok
                {
                    Id = payloadId
                };
                var sendPayload = _serializer.Serialize(ok);
                sendQueue.EnqueueFireAndForget(sendPayload);
            }
        }

        private void SendReceivePayloadError(Guid clientId, Guid payloadId, string message)
        {
            if (_clientStore.TryGet(clientId, out var sendQueue))
            {
                var error = new Error
                {
                    Id = payloadId,
                    Message = message
                };
                var sendPayload = _serializer.Serialize(error);
                sendQueue.EnqueueFireAndForget(sendPayload);
            }
        }
    }
}