using System;
using MessageBroker.SocketServer;
using MessageBroker.SocketServer.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Classes;
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