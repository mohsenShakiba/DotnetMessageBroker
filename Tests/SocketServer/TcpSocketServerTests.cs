// using System;
// using System.Net;
// using System.Net.Sockets;
// using MessageBroker.Core.Socket;
// using MessageBroker.Core.Socket.Client;
// using MessageBroker.Core.Socket.Server;
// using MessageBroker.Models;
// using MessageBroker.Serialization;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Xunit;
//
// namespace Tests.SocketServer
// {
//     public class TcpSocketServerTests
//     {
//         /// <summary>
//         ///     this test will verify that add and remove methods of session resolver is called by tcp socket server
//         ///     when a client is connected or disconnected
//         /// </summary>
//         [Fact]
//         public void TcpSocketServerSessionResolverTest()
//         {
//             #region Arrange
//
//             var sessionResolverMock = new Mock<ISessionResolver>();
//             var socketEventProcessorMock = new Mock<ISocketEventProcessor>();
//             var loggerFactory = LoggerFactory.Create(_ => { });
//
//             var services = new ServiceCollection();
//
//             services.AddSingleton(i => sessionResolverMock.Object);
//             services.AddSingleton(i => socketEventProcessorMock.Object);
//             services.AddSingleton(i => loggerFactory);
//             services.AddSingleton<TcpSocketServer>();
//
//             var serviceProvider = services.BuildServiceProvider();
//
//             #endregion
//
//             #region Act
//
//             var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8001);
//
//             var server = serviceProvider.GetRequiredService<TcpSocketServer>();
//             server.Start(ipEndPoint);
//
//             var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//             client.Connect(ipEndPoint);
//             client.Disconnect(true);
//
//             #endregion
//
//             #region Assert
//
//             sessionResolverMock.Verify(i => i.Add(It.IsAny<IClientSession>()));
//             sessionResolverMock.Verify(i => i.Remove(It.IsAny<Guid>()));
//
//             #endregion
//         }
//
//         /// <summary>
//         ///     this test will verify that IEventProcessor methods are called when:
//         ///     client connects
//         ///     client disconnects
//         ///     data is received
//         /// </summary>
//         [Fact]
//         public void TcpSocketServerEventProcessorTest()
//         {
//             #region Arrange
//
//             var sessionResolverMock = new Mock<ISessionResolver>();
//             var socketEventProcessorMock = new Mock<ISocketEventProcessor>();
//             var loggerFactory = LoggerFactory.Create(_ => { });
//
//             var services = new ServiceCollection();
//
//             services.AddSingleton(i => sessionResolverMock.Object);
//             services.AddSingleton(i => socketEventProcessorMock.Object);
//             services.AddSingleton(i => loggerFactory);
//             services.AddSingleton<TcpSocketServer>();
//             services.AddSingleton<ISerializer, Serializer>();
//
//             var serviceProvider = services.BuildServiceProvider();
//
//             #endregion
//
//             #region Act
//
//             var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8002);
//
//             var server = serviceProvider.GetRequiredService<TcpSocketServer>();
//             server.Start(ipEndPoint);
//
//             var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//             client.Connect(ipEndPoint);
//
//             var serializer = serviceProvider.GetRequiredService<ISerializer>();
//
//             var register = new ConfigureSubscription {Concurrency = 10, Id = Guid.Empty};
//             var sendPayload = serializer.ToSendPayload(register);
//             client.Send(sendPayload.Data.Span);
//
//             client.Disconnect(true);
//
//             #endregion
//
//             #region Assert
//
//             socketEventProcessorMock.Verify(i => i.ClientConnected(It.IsAny<Guid>()));
//             socketEventProcessorMock.Verify(i => i.DataReceived(It.IsAny<Guid>(), It.IsAny<Memory<byte>>()));
//             socketEventProcessorMock.Verify(i => i.ClientDisconnected(It.IsAny<Guid>()));
//
//             #endregion
//         }
//     }
// }