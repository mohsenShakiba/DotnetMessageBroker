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

        private ISerializer _serializer;

        public ParserTests()
        {
            var bufferPool = new DefaultBufferPool();
            _serializer = new DefaultSerializer(bufferPool);
        }

        [Fact]
        public void TestParseAck()
        {
            var ack = new Ack(Guid.NewGuid());

            var b = _serializer.Serialize(ack);

            var convertedAck = _serializer.Deserialize(b) as Ack;

            Assert.IsType<Ack>(convertedAck);
            Assert.Equal(ack.Id, convertedAck.Id);
        }

        [Fact]
        public void TestParseNack()
        {
            var nack = new Nack(Guid.NewGuid());

            var b = _serializer.Serialize(nack);

            var convertedNack = _serializer.Deserialize(b) as Nack;

            Assert.IsType<Nack>(convertedNack);
            Assert.Equal(nack.Id, convertedNack.Id);
        }

        [Fact]
        public void TestParseMessage()
        {
            var msg = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("DATA"));

            var b = _serializer.Serialize(msg);

            var convertedMsg = _serializer.Deserialize(b) as Message;

            Assert.IsType<Message>(convertedMsg);
            Assert.Equal(msg.Id, convertedMsg.Id);
            Assert.Equal(msg.Route, convertedMsg.Route);
            Assert.Equal(Encoding.UTF8.GetString(msg.Data), Encoding.UTF8.GetString(convertedMsg.Data.AsMemory().Trim(Encoding.UTF8.GetBytes("\0")).ToArray()));
        }

        [Fact]
        public void TestParseListen()
        {
            var listen = new Listen(Guid.NewGuid(), "TEST");

            var b = _serializer.Serialize(listen);

            var convertedListen = _serializer.Deserialize(b) as Listen;

            Assert.IsType<Listen>(convertedListen);
            Assert.Equal(listen.Id, convertedListen.Id);
            Assert.Equal(listen.Route, convertedListen.Route);
        }

        [Fact]
        public void TestParseUnlisten()
        {
            var unlisten = new Unlisten(Guid.NewGuid(), "TEST");

            var b = _serializer.Serialize(unlisten);

            var convertedUnlisten = _serializer.Deserialize(b) as Unlisten;

            Assert.IsType<Unlisten>(convertedUnlisten);
            Assert.Equal(unlisten.Id, convertedUnlisten.Id);
            Assert.Equal(unlisten.Route, convertedUnlisten.Route);
        }

        [Fact]
        public void TestParseSubscribe()
        {
            var subscribe = new Subscribe(Guid.NewGuid(), 10);

            var b = _serializer.Serialize(subscribe);

            var convertedSubscribe = _serializer.Deserialize(b) as Subscribe;

            Assert.IsType<Subscribe>(convertedSubscribe);
            Assert.Equal(subscribe.Id, convertedSubscribe.Id);
            Assert.Equal(subscribe.Concurrency, convertedSubscribe.Concurrency);
        }

    }
}
