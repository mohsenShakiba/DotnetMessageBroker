using System;
using System.Text;
using MessageBroker.Core;
using MessageBroker.Core.InternalEventChannel;
using MessageBroker.Models;
using MessageBroker.Serialization;
using MessageBroker.SocketServer.Abstractions;
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
            var eventChannel = new EventChannel();

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

            var sendPayloadOne = serializer.ToSendPayload(messageOne);
            var sendPayloadTwo = serializer.ToSendPayload(messageTwo);

            var sendQueue = new SendQueue(session.Object, eventChannel);
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
            sendQueue.ReleaseOne(messageOne.Id);

            // verify send was called
            session.Verify(session => session.SendAsync(It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void TestReleaseWhenMessageDoesNotExists()
        {
            var serializer = new Serializer();
            var session = new Mock<IClientSession>();
            var eventChannel = new EventChannel();
            var sendQueue = new SendQueue(session.Object, eventChannel);
            sendQueue.Configure(1, false, 1);
            var randomId = Guid.NewGuid();

            sendQueue.ReleaseOne(randomId);

            Assert.Equal(1, sendQueue.CurrentConcurrency);
        }
    }
}