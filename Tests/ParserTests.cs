using MessageBroker.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Extensions;
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

            var b = ToBinary(ack);

            var convertedAck = _parser.Parse(b) as Ack;

            Assert.IsType<Ack>(convertedAck);
            Assert.Equal(ack.MsgId, convertedAck.MsgId);
        }

        [Fact]
        public void TestParseMessage()
        {
            var msg = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("DATA"));

            var b = ToBinary(msg);

            var convertedMsg = _parser.Parse(b) as Message;

            Assert.IsType<Message>(convertedMsg);
            Assert.Equal(msg.Id, convertedMsg.Id);
            Assert.Equal(msg.Route, convertedMsg.Route);
            Assert.Equal(msg.Data, convertedMsg.Data);
        }

        private byte[] ToBinary(Ack ack)
        {
            var buff = new List<byte>();
            
            buff.AddWithDelimiter(MessageTypes.Ack);
            buff.AddWithDelimiter(ack.MsgId);

            return buff.ToArray();
        }

        private byte[] ToBinary(Message msg)
        {
            var buff = new List<byte>();

            buff.AddWithDelimiter(MessageTypes.Message);
            buff.AddWithDelimiter(msg.Id);
            buff.AddWithDelimiter(msg.Route);
            buff.AddWithDelimiter(msg.Data);

            return buff.ToArray();
        }

    }
}
