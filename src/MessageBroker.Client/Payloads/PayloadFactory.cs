using System;
using System.Runtime.CompilerServices;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Models;
using MessageBroker.Common.Serialization;

[assembly: InternalsVisibleTo("Tests")]

namespace MessageBroker.Client.Payloads
{
    /// <inheritdoc />
    internal class PayloadFactory : IPayloadFactory
    {
        private readonly ISerializer _serializer;

        public PayloadFactory(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public SerializedPayload NewSubscribeTopic(string topicName)
        {
            var payload = new SubscribeTopic
            {
                Id = Guid.NewGuid(),
                TopicName = topicName
            };

            return _serializer.Serialize(payload);
        }

        public SerializedPayload NewUnsubscribeTopic(string topicName)
        {
            var payload = new UnsubscribeTopic
            {
                Id = Guid.NewGuid(),
                TopicName = topicName
            };

            return _serializer.Serialize(payload);
        }

        public SerializedPayload NewAck(Guid messageId)
        {
            var payload = new Ack
            {
                Id = messageId
            };

            return _serializer.Serialize(payload);
        }

        public SerializedPayload NewNack(Guid messageId)
        {
            var payload = new Nack
            {
                Id = messageId
            };

            return _serializer.Serialize(payload);
        }

        public SerializedPayload NewMessage(byte[] data, string route)
        {
            var payload = new Message
            {
                Id = Guid.NewGuid(),
                Data = data,
                Route = route
            };

            return _serializer.Serialize(payload);
        }

        public SerializedPayload NewDeclareTopic(string name, string route)
        {
            var payload = new TopicDeclare
            {
                Id = Guid.NewGuid(),
                Name = name,
                Route = route
            };

            return _serializer.Serialize(payload);
        }

        public SerializedPayload NewDeleteTopic(string name)
        {
            var payload = new TopicDelete
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            return _serializer.Serialize(payload);
        }

        public SerializedPayload NewConfigureClient(int prefetchCount)
        {
            var payload = new ConfigureClient
            {
                Id = Guid.NewGuid(),
                PrefetchCount = prefetchCount
            };

            return _serializer.Serialize(payload);
        }
    }
}