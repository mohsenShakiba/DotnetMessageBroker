using System;
using System.Text;
using MessageBroker.Common.Models;
using MessageBroker.Common.Serialization;
using Tests.Classes;
using Xunit;

namespace Tests.Serialization
{
    public class SerializerTests
    {
        private readonly Deserializer _deserializer;
        private readonly Serializer _serializer;

        public SerializerTests()
        {
            _serializer = new Serializer();
            _deserializer = new Deserializer();
        }

        [Fact]
        public void Serialization_Message_MatchResults()
        {
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                Route = RandomGenerator.GenerateString(10),
                Data = Encoding.UTF8.GetBytes(RandomGenerator.GenerateString(10))
            };

            var b = _serializer.Serialize(msg);

            var result = _deserializer.ToMessage(b.DataWithoutSize);

            Assert.Equal(msg.Id, result.Id);
            Assert.Equal(msg.Route, result.Route);
            Assert.Equal(Encoding.UTF8.GetString(msg.Data.Span), Encoding.UTF8.GetString(result.Data.Span));
        }

        [Fact]
        public void Serialization_TopicMessage_MatchResults()
        {
            var msg = new TopicMessage
            {
                Id = Guid.NewGuid(),
                TopicName = RandomGenerator.GenerateString(10),
                Route = RandomGenerator.GenerateString(10),
                Data = Encoding.UTF8.GetBytes(RandomGenerator.GenerateString(10))
            };

            var b = _serializer.Serialize(msg);

            var result = _deserializer.ToTopicMessage(b.DataWithoutSize);

            Assert.Equal(msg.Id, result.Id);
            Assert.Equal(msg.TopicName, result.TopicName);
            Assert.Equal(msg.Route, result.Route);
            Assert.Equal(Encoding.UTF8.GetString(msg.Data.Span), Encoding.UTF8.GetString(result.Data.Span));
        }

        [Fact]
        public void Serialization_Ack_MatchResults()
        {
            var ack = new Ack {Id = Guid.NewGuid()};

            var b = _serializer.Serialize(ack);

            var result = _deserializer.ToAck(b.DataWithoutSize);

            Assert.Equal(ack.Id, result.Id);
        }

        [Fact]
        public void Serialization_NackMatchResults()
        {
            var nack = new Nack {Id = Guid.NewGuid()};

            var b = _serializer.Serialize(nack);

            var result = _deserializer.ToNack(b.DataWithoutSize);

            Assert.Equal(nack.Id, result.Id);
        }

        [Fact]
        public void Serialization_SubscribeTopic_MatchResults()
        {
            var subscribeQueue = new SubscribeTopic
            {
                Id = Guid.NewGuid(),
                TopicName = RandomGenerator.GenerateString(10)
            };

            var b = _serializer.Serialize(subscribeQueue);

            var result = _deserializer.ToSubscribeTopic(b.DataWithoutSize);

            Assert.Equal(subscribeQueue.Id, result.Id);
            Assert.Equal(subscribeQueue.TopicName, result.TopicName);
        }

        [Fact]
        public void Serialization_UnsubscribeTopic_MatchResults()
        {
            var unsubscribeQueue = new UnsubscribeTopic
            {
                Id = Guid.NewGuid(),
                TopicName = RandomGenerator.GenerateString(10)
            };

            var b = _serializer.Serialize(unsubscribeQueue);

            var result = _deserializer.ToUnsubscribeTopic(b.DataWithoutSize);

            Assert.Equal(unsubscribeQueue.Id, result.Id);
            Assert.Equal(unsubscribeQueue.TopicName, result.TopicName);
        }


        [Fact]
        public void Serialization_TopicDeclare_MatchResults()
        {
            var queue = new TopicDeclare
            {
                Id = Guid.NewGuid(),
                Name = RandomGenerator.GenerateString(10),
                Route = RandomGenerator.GenerateString(10)
            };

            var b = _serializer.Serialize(queue);

            var result = _deserializer.ToTopicDeclare(b.DataWithoutSize);

            Assert.Equal(queue.Id, result.Id);
            Assert.Equal(queue.Name, result.Name);
            Assert.Equal(queue.Route, result.Route);
        }

        [Fact]
        public void Serialization_TopicDelete_MatchResults()
        {
            var queue = new TopicDelete {Id = Guid.NewGuid(), Name = RandomGenerator.GenerateString(10)};

            var b = _serializer.Serialize(queue);

            var result = _deserializer.ToTopicDelete(b.DataWithoutSize);

            Assert.Equal(queue.Id, result.Id);
            Assert.Equal(queue.Name, result.Name);
        }

        [Fact]
        public void Serialization_Ok_MatchResults()
        {
            var ok = new Ok {Id = Guid.NewGuid()};

            var b = _serializer.Serialize(ok);

            var result = _deserializer.ToOk(b.DataWithoutSize);

            Assert.Equal(ok.Id, result.Id);
        }

        [Fact]
        public void Serialization_Error_MatchResults()
        {
            var error = new Error {Id = Guid.NewGuid(), Message = RandomGenerator.GenerateString(10)};

            var b = _serializer.Serialize(error);

            var result = _deserializer.ToError(b.DataWithoutSize);

            Assert.Equal(error.Id, result.Id);
            Assert.Equal(error.Message, result.Message);
        }

        [Fact]
        public void Serialization_ConfigureClient_MatchResults()
        {
            var configureClient = new ConfigureClient {Id = Guid.NewGuid(), PrefetchCount = 10};

            var b = _serializer.Serialize(configureClient);

            var result = _deserializer.ToConfigureClient(b.DataWithoutSize);

            Assert.Equal(configureClient.Id, result.Id);
            Assert.Equal(configureClient.PrefetchCount, result.PrefetchCount);
        }
    }
}