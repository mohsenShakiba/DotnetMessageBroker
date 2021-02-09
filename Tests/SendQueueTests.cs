using System;
using System.Text;
using MessageBroker.Core;
using MessageBroker.Core.Socket.Client;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Moq;
using Xunit;

namespace Tests
{
    public class SendQueueTests
    {
        [Fact]
        public void TestEnqueuWhenFull()
        {
            var serializer = new Serializer();
            var session = new Mock<IClientSession>();

            var messageOne = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = Encoding.UTF8.GetBytes("TEST")
            };

            var messageTwo = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = Encoding.UTF8.GetBytes("TEST")
            };

            var sendPayloadOne = serializer.Serialize(messageOne);
            var sendPayloadTwo = serializer.Serialize(messageTwo);

            var sendQueue = new SendQueue(session.Object);
            sendQueue.Configure(1, false);

            // enqueu first message
            sendQueue.Enqueue(sendPayloadOne);

            // make sure send method was called
            session.Verify(session => session.SendAsync(It.IsAny<Memory<byte>>()));

            // enqueue second message
            sendQueue.Enqueue(sendPayloadTwo);

            // make sure the session has 
            Assert.Equal(1, sendQueue.CurrentConcurrency);

            // when release is called, send method of session should be called 
            sendQueue.OnMessageAckReceived(messageOne.Id);

            // verify send was called
            session.Verify(session => session.SendAsync(It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void TestReleaseWhenMessageDoesNotExists()
        {
            var serializer = new Serializer();
            var session = new Mock<IClientSession>();
            var sendQueue = new SendQueue(session.Object);
            sendQueue.Configure(1, false, 1);
            var randomId = Guid.NewGuid();

            sendQueue.OnMessageAckReceived(randomId);

            Assert.Equal(1, sendQueue.CurrentConcurrency);
        }
    }
}