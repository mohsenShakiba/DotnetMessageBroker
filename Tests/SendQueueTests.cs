using MessageBroker.Core;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Server;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class SendQueueTests
    {

        public SendQueueTests()
        {

        }

        [Fact]
        public void TestEnqueuWhenNotFull()
        {
            var pareser = new Parser();
            var session = new Mock<IClientSession>();
            var message = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("TEST"));

            var sendQueue = new SendQueue(session.Object);

            sendQueue.Enqueue(message);

            // make sure the session send was called
            session.Verify(session => session.Send(It.IsAny<byte[]>()));

            // make sure the session has 
            Assert.Equal(1, sendQueue.CurrentCuncurrency);
            Assert.Equal(message.Id, sendQueue.PendingMessages.First());
        }

        [Fact]
        public void TestEnqueuWhenFull()
        {
            var pareser = new Parser();
            var session = new Mock<IClientSession>();
            var message = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("TEST"));

            var sendQueue = new SendQueue(session.Object);

            sendQueue.Enqueue(message);

            // make sure the session send was not called
            session.VerifyNoOtherCalls();

            // make sure the session has 
            Assert.Equal(10, sendQueue.CurrentCuncurrency);
        }

    }
}
