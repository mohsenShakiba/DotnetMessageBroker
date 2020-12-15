using MessageBroker.Messages;
using System;
using System.Text;
using Xunit;

namespace Tests
{
    public class ParserTests
    {

        private Parser _parser;

        public ParserTests()
        {
            _parser = new Parser();
        }

        [Fact]
        public void TestParseAck()
        {
            var ack = new Ack(Guid.NewGuid());

            var b = _parser.ToBinary(ack);

            var convertedAck = _parser.Parse(b) as Ack;

            Assert.IsType<Ack>(convertedAck);
            Assert.Equal(ack.MsgId, convertedAck.MsgId);
        }

        [Fact]
        public void TestParseMessage()
        {
            var msg = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("DATA"));

            var b = _parser.ToBinary(msg);

            var convertedMsg = _parser.Parse(b) as Message;

            Assert.IsType<Message>(convertedMsg);
            Assert.Equal(msg.Id, convertedMsg.Id);
            Assert.Equal(msg.Route, convertedMsg.Route);
            Assert.Equal(Encoding.UTF8.GetString(msg.Data), Encoding.UTF8.GetString(convertedMsg.Data.AsMemory().Trim(Encoding.UTF8.GetBytes("\0")).ToArray()));
        }

        [Fact]
        public void TestParseListen()
        {
            var listen = new Listen("TEST");

            var b = _parser.ToBinary(listen);

            var convertedListen = _parser.Parse(b) as Listen;

            Assert.IsType<Listen>(convertedListen);
            Assert.Equal(listen.Route, convertedListen.Route);
        }

    }
}
