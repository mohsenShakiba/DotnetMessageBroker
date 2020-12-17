using MessageBroker.Core;
using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Abstractions;
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

        [Fact]
        public void TestEnqueuWhenFull()
        {
            var serializer = new DefaultSerializer();
            var session = new Mock<IClientSession>();
            var messageOne = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("TEST"));
            var messageTwo = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("TEST"));

            var sendQueue = new SendQueue(session.Object, serializer, 1, 0);

            // enqueu first message
            sendQueue.Enqueue(messageOne);

            // make sure send method was called
            session.Verify(session => session.Send(It.IsAny<byte[]>()));

            // enqueue second message
            sendQueue.Enqueue(messageTwo);

            // make sure the session send was not called
            session.VerifyNoOtherCalls();

            // make sure the session has 
            Assert.Equal(1, sendQueue.CurrentCuncurrency);

            // when release is called, send method of session should be called 
            sendQueue.ReleaseOne(messageOne.Id);

            // verify send was called
            session.Verify(session => session.Send(It.IsAny<byte[]>()));
        }

        [Fact]
        public void TestReleaseWhenMessageDoesNotExists()
        {
            var session = new Mock<IClientSession>();
            var serializer = new DefaultSerializer();
            var sendQueue = new SendQueue(session.Object, serializer, 1, 1);
            var randomId = Guid.NewGuid();

            sendQueue.ReleaseOne(randomId);

            Assert.Equal(1, sendQueue.CurrentCuncurrency);
        }


    }
}
