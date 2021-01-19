using MessageBroker.Core.Serialize;
using System;
using System.Text;
using MessageBroker.Core.Payloads;
using Xunit;

namespace Tests
{
    public class ParserTests
    {

        private Serializer _serializer;

        public ParserTests()
        {
            _serializer = new Serializer();
        }

        [Fact]
        public void TestParseAck()
        {
            var ack = new Ack { Id = Guid.NewGuid() };

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
            Assert.Equal(Encoding.UTF8.GetString(msg.Data.ToArray()), Encoding.UTF8.GetString(convertedMsg.Data.Trim(Encoding.UTF8.GetBytes("\0")).Trim(Encoding.UTF8.GetBytes("\n")).ToArray()));
        }

        [Fact]
        public void TestParseListen()
        {
            var listen = new SubscribeQueue { Id = Guid.NewGuid(), QueueName = "TEST" };

            var b = _serializer.ToSendPayload(listen);

            var convertedListen = _serializer.ToListenRoute(b.DataWithoutSize);

            Assert.Equal(listen.Id, convertedListen.Id);
            Assert.Equal(listen.QueueName, convertedListen.QueueName);
        }


        [Fact]
        public void TestParseSubscribe()
        {
            var subscribe = new Register { Id = Guid.NewGuid(), Concurrency = 10 };

            var b = _serializer.ToSendPayload(subscribe);

            var convertedSubscribe = _serializer.ToSubscribe(b.DataWithoutSize);

            Assert.Equal(subscribe.Id, convertedSubscribe.Id);
            Assert.Equal(subscribe.Concurrency, convertedSubscribe.Concurrency);
        }

        [Fact]
        public void TestParseQueueDeclare()
        {
            var queue = new QueueDeclare { Id = Guid.NewGuid(), Name = "TEST_QUEUE", Route = "TEST_PATH"};

            var b = _serializer.ToSendPayload(queue);

            var convertedQueueDeclare = _serializer.ToQueueDeclareModel(b.DataWithoutSize);

            Assert.Equal(queue.Id, convertedQueueDeclare.Id);
            Assert.Equal(queue.Name, convertedQueueDeclare.Name);
            Assert.Equal(queue.Route, convertedQueueDeclare.Route);
        }

        [Fact]
        public void TestParseQueueDelete()
        {
            var queue = new QueueDelete { Id = Guid.NewGuid(), Name = "TEST_QUEUE" };

            var b = _serializer.ToSendPayload(queue);

            var convertedQueueDelete = _serializer.ToQueueDeleteModel(b.DataWithoutSize);

            Assert.Equal(queue.Id, convertedQueueDelete.Id);
            Assert.Equal(queue.Name, convertedQueueDelete.Name);
        }

    }
}
