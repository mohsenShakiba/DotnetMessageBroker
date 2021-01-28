using System;
using System.Text;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Xunit;

namespace Tests
{
    public class ParserTests
    {
        private readonly Serializer _serializer;

        public ParserTests()
        {
            _serializer = new Serializer();
        }

        [Fact]
        public void TestParseAck()
        {
            var ack = new Ack {Id = Guid.NewGuid()};

            var b = _serializer.ToSendPayload(ack);

            var convertedAck = _serializer.ToAck(b.DataWithoutSize);

            Assert.Equal(ack.Id, convertedAck.Id);
        }

        [Fact]
        public void TestParseMessage()
        {
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = Encoding.UTF8.GetBytes("DATA")
            };

            var b = _serializer.ToSendPayload(msg);

            var convertedMsg = _serializer.ToMessage(b.DataWithoutSize);

            Assert.Equal(msg.Id, convertedMsg.Id);
            Assert.Equal(msg.Route, convertedMsg.Route);
            Assert.Equal(Encoding.UTF8.GetString(msg.Data.ToArray()),
                Encoding.UTF8.GetString(convertedMsg.Data.Trim(Encoding.UTF8.GetBytes("\0"))
                    .Trim(Encoding.UTF8.GetBytes("\n")).ToArray()));
        }

        [Fact]
        public void TestParseSubscribeQueue()
        {
            var subscribeQueue = new SubscribeQueue {Id = Guid.NewGuid(), QueueName = "TEST"};

            var b = _serializer.ToSendPayload(subscribeQueue);

            var converted = _serializer.ToSubscribeQueue(b.DataWithoutSize);

            Assert.Equal(subscribeQueue.Id, converted.Id);
            Assert.Equal(subscribeQueue.QueueName, converted.QueueName);
        }

        [Fact]
        public void TestParseUnSubscribeQueue()
        {
            var unsubscribeQueue = new SubscribeQueue {Id = Guid.NewGuid(), QueueName = "TEST"};

            var b = _serializer.ToSendPayload(unsubscribeQueue);

            var converted = _serializer.ToSubscribeQueue(b.DataWithoutSize);

            Assert.Equal(unsubscribeQueue.Id, converted.Id);
            Assert.Equal(unsubscribeQueue.QueueName, converted.QueueName);
        }


        [Fact]
        public void TestParseConfigureSubscription()
        {
            var configureSubscription = new ConfigureSubscription {Id = Guid.NewGuid(), Concurrency = 10};

            var b = _serializer.ToSendPayload(configureSubscription);

            var convertedConfigureSubscription = _serializer.ToConfigureSubscription(b.DataWithoutSize);

            Assert.Equal(configureSubscription.Id, convertedConfigureSubscription.Id);
            Assert.Equal(configureSubscription.Concurrency, convertedConfigureSubscription.Concurrency);
            Assert.Equal(configureSubscription.AutoAck, convertedConfigureSubscription.AutoAck);
        }

        [Fact]
        public void TestParseQueueDeclare()
        {
            var queue = new QueueDeclare {Id = Guid.NewGuid(), Name = "TEST_QUEUE", Route = "TEST_PATH"};

            var b = _serializer.ToSendPayload(queue);

            var convertedQueueDeclare = _serializer.ToQueueDeclareModel(b.DataWithoutSize);

            Assert.Equal(queue.Id, convertedQueueDeclare.Id);
            Assert.Equal(queue.Name, convertedQueueDeclare.Name);
            Assert.Equal(queue.Route, convertedQueueDeclare.Route);
        }

        [Fact]
        public void TestParseQueueDelete()
        {
            var queue = new QueueDelete {Id = Guid.NewGuid(), Name = "TEST_QUEUE"};

            var b = _serializer.ToSendPayload(queue);

            var convertedQueueDelete = _serializer.ToQueueDeleteModel(b.DataWithoutSize);

            Assert.Equal(queue.Id, convertedQueueDelete.Id);
            Assert.Equal(queue.Name, convertedQueueDelete.Name);
        }
    }
}