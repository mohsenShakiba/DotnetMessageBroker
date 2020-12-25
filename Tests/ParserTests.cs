using MessageBroker.Core.BufferPool;
using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using MessageBroker.Messages;
using System;
using System.Text;
using Xunit;

namespace Tests
{
    public class ParserTests
    {

        private DefaultSerializer _serializer;

        public ParserTests()
        {
            var bufferPool = new DefaultBufferPool();
            _serializer = new DefaultSerializer(bufferPool);
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
            Assert.Equal(Encoding.UTF8.GetString(msg.Data.ToArray()), Encoding.UTF8.GetString(convertedMsg.Data.Trim(Encoding.UTF8.GetBytes("\0")).ToArray()));
        }

        [Fact]
        public void TestParseListen()
        {
            var listen = new Listen { Id = Guid.NewGuid(), Route = "TEST" };

            var b = _serializer.ToSendPayload(listen);

            var convertedListen = _serializer.ToListenRoute(b.DataWithoutSize);

            Assert.Equal(listen.Id, convertedListen.Id);
            Assert.Equal(listen.Route, convertedListen.Route);
        }


        [Fact]
        public void TestParseSubscribe()
        {
            var subscribe = new Subscribe { Id = Guid.NewGuid(), Concurrency = 10 };

            var b = _serializer.ToSendPayload(subscribe);

            var convertedSubscribe = _serializer.ToSubscribe(b.DataWithoutSize);

            Assert.Equal(subscribe.Id, convertedSubscribe.Id);
            Assert.Equal(subscribe.Concurrency, convertedSubscribe.Concurrency);
        }

    }
}
