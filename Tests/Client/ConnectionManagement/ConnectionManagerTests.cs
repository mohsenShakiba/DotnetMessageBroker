using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.SocketWrapper;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Client.ConnectionManagement
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void Connect_ConnectionFailedForFirstTimeAndSucceedsTheSecondTime_ConnectionIsEstablished()
        {
            // create mocks 
            var clientSession = new Mock<IClientSession>();
            var receiveDataProcessor = new Mock<IReceiveDataProcessor>();
            var tcpSocket = new Mock<ITcpSocket>();
            
            // setup mocks
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Throws<Exception>();
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Pass();
            
            // declare variables
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = It.IsAny<IPEndPoint>(),
                RetryOnFailure = true
            };
            
            // setup UUT
            var connectionManager = new ConnectionManager(clientSession.Object,
                    receiveDataProcessor.Object);
            
            connectionManager.SetAlternativeTcpSocketForTesting(tcpSocket.Object);
            connectionManager.Connect(connectionConfiguration);
            
            // verify connection manager is in connected state
            Assert.True(connectionManager.Connected);
            Assert.NotNull(connectionManager.ClientSession);
        }

        [Fact]
        public void Connect_ConnectionSucceedsForFirstTimeButDisconnectsAfterwards_ConnectionIsReEstablished()
        {
            // create mocks 
            var clientSession = new Mock<IClientSession>();
            var receiveDataProcessor = new Mock<IReceiveDataProcessor>();
            var tcpSocket = new Mock<ITcpSocket>();
            
            // setup mocks
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Pass();
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Throws<Exception>();
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Pass();

            // declare variables
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = It.IsAny<IPEndPoint>(),
                RetryOnFailure = true
            };
            
            // setup UUT
            var connectionManager = new ConnectionManager(clientSession.Object,
                receiveDataProcessor.Object);
            
            connectionManager.SetAlternativeTcpSocketForTesting(tcpSocket.Object);
            connectionManager.Connect(connectionConfiguration);
            
            // force the client to manager to think that the client is closed
            connectionManager.ClientDisconnected(clientSession.Object);
            
            // verify connection manager is in connected state
            Assert.True(connectionManager.Connected);
            Assert.NotNull(connectionManager.ClientSession);
            
        }

        [Fact]
        public void Connect_ConnectionFailsWhenReconnectIsFalse_ExceptionIsThrown()
        {
            // create mocks 
            var clientSession = new Mock<IClientSession>();
            var receiveDataProcessor = new Mock<IReceiveDataProcessor>();
            var tcpSocket = new Mock<ITcpSocket>();
            
            // setup mocks
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Throws<Exception>();

            // declare variables
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = It.IsAny<IPEndPoint>(),
                RetryOnFailure = false
            };
            
            // setup UUT
            var connectionManager = new ConnectionManager(clientSession.Object,
                receiveDataProcessor.Object);
            
            connectionManager.SetAlternativeTcpSocketForTesting(tcpSocket.Object);

            // verify it throws exception
            Assert.Throws<Exception>(() =>
            {
                connectionManager.Connect(connectionConfiguration);
            });
        }

        [Fact]
        public void Disconnect_SocketIsClosed()
        {
            var clientSession = new Mock<IClientSession>();
            var receiveDataProcessor = new Mock<IReceiveDataProcessor>();
            var tcpSocket = new Mock<ITcpSocket>();
            
            // declare variables
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = It.IsAny<IPEndPoint>(),
                RetryOnFailure = false
            };
            
            // setup UUT
            var connectionManager = new ConnectionManager(clientSession.Object,
                receiveDataProcessor.Object);
            
            connectionManager.SetAlternativeTcpSocketForTesting(tcpSocket.Object);
            connectionManager.Connect(connectionConfiguration);
            connectionManager.Disconnect();
            
            tcpSocket.Verify(t => t.Disconnect(It.IsAny<bool>()));
            clientSession.Verify(cs => cs.Close());
        }
    }
}