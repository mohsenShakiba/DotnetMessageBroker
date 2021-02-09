// using System;
// using MessageBroker.Core.Socket.Client;
// using Moq;
// using Xunit;
//
// namespace Tests.SocketServer
// {
//     public class SessionResolverTests
//     {
//         [Fact]
//         public void TestAddingGettingAndRemovingSessionInSessionResolver()
//         {
//
//             var sessionMock = new Mock<IClientSession>();
//
//             var sessionGuid = Guid.NewGuid();
//
//             sessionMock.SetupGet(s => s.Id).Returns(sessionGuid);
//
//             sessionResolver.Add(sessionMock.Object);
//
//             var sessionWhenExists = sessionResolver.Resolve(sessionGuid);
//             sessionResolver.Remove(sessionGuid);
//             var sessionWhenEmpty = sessionResolver.Resolve(sessionGuid);
//
//             Assert.Equal(sessionGuid, sessionWhenExists.SessionId);
//             Assert.Null(sessionWhenEmpty);
//         }
//     }
// }