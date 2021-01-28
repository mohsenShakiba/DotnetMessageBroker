using System;
using System.Text;
using MessageBroker.Core;
using MessageBroker.Core.InternalEventChannel;
using MessageBroker.Core.Queues;
using MessageBroker.Models;
using MessageBroker.Serialization;
using MessageBroker.SocketServer.Abstractions;
using Moq;
using Xunit;

namespace Tests
{
    public class MessageDispatcherTests
    {
        [Fact]
        public void TestDispatchMessage()
        {
            var sessionId = Guid.NewGuid();
            var session = new Mock<IClientSession>();
            var sessionResolver = new Mock<ISessionResolver>();
            var eventChannel = new EventChannel();
            var serializer = new Serializer();

            session.Setup(s => s.SessionId).Returns(sessionId);
            sessionResolver.Setup(sr => sr.Resolve(It.IsAny<Guid>())).Returns(session.Object);

            var dispatcher = new MessageDispatcher(sessionResolver.Object, eventChannel);

            dispatcher.AddSendQueue(sessionId);

            var originalMessage = new Message
                {Id = Guid.NewGuid(), Route = "TEST", Data = Encoding.UTF8.GetBytes("TEST")};

            var originalMessageSendData = serializer.ToSendPayload(originalMessage);

            dispatcher.Dispatch(originalMessageSendData, session.Object.SessionId);

            // make sure the send queue is created and it's the same as session
            var sendQueue = dispatcher.GetSendQueue(sessionId);
            Assert.Equal(session.Object, sendQueue.Session);

            // make sure the resolve method of session resolver was called
            sessionResolver.Verify(sr => sr.Resolve(It.IsAny<Guid>()));

            // make sure the send method of session was called
            session.Verify(s => s.SendAsync(It.IsAny<Memory<byte>>()));
        }
    }
}