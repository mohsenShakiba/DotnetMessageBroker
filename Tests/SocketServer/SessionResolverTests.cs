using System;
using MessageBroker.SocketServer;
using MessageBroker.SocketServer.Abstractions;
using Moq;
using Xunit;

namespace Tests.SocketServer
{
    public class SessionResolverTests
    {
        [Fact]
        public void TestAddingGettingAndRemovingSessionInSessionResolver()
        {
            var sessionResolver = new SessionResolver();

            var sessionMock = new Mock<IClientSession>();

            var sessionGuid = Guid.NewGuid();

            sessionMock.SetupGet(s => s.SessionId).Returns(sessionGuid);

            sessionResolver.Add(sessionMock.Object);

            var sessionWhenExists = sessionResolver.Resolve(sessionGuid);
            sessionResolver.Remove(sessionGuid);
            var sessionWhenEmpty = sessionResolver.Resolve(sessionGuid);

            Assert.Equal(sessionGuid, sessionWhenExists.SessionId);
            Assert.Null(sessionWhenEmpty);
        }
    }
}