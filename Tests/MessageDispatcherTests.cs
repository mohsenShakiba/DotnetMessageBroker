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
    public class MessageDispatcherTests
    {
        [Fact]
        public void TestDispatchMessage()
        {
            var sessionId = Guid.NewGuid();
            var session = new Mock<IClientSession>(); 
            var sessionResolver = new Mock<ISessionResolver>();

            session.Setup(s => s.SessionId).Returns(sessionId);
            sessionResolver.Setup(sr => sr.ResolveSession(It.IsAny<Guid>())).Returns(session.Object);

            var dispatcher = new MessageDispatcher(sessionResolver.Object);

            var message = new Message(Guid.NewGuid(), "TEST", Encoding.UTF8.GetBytes("TEST"));

            dispatcher.Dispatch(message, new Guid[] { session.Object.SessionId });

            // make sure the send queue is created and it's the same as session
            var sendQueue = dispatcher.GetSendQueue(sessionId);
            Assert.Equal(session.Object, sendQueue.Session);

            // make sure the resolve method of session resolver was called
            sessionResolver.Verify(sr => sr.ResolveSession(It.IsAny<Guid>()));

            // make sure the send method of session was called
            session.Verify(s => s.Send(It.IsAny<byte[]>()));
        }

    }
}
