using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Binary;
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
            var receiveDataProcessor = new Mock<IReceiveDataProcessor>();
            var binaryDataProcessor = new Mock<IBinaryDataProcessor>();
            var tcpSocket = new Mock<ITcpSocket>();
            
            // setup mocks
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Throws<Exception>();
            tcpSocket.SetupSequence(s => s.Connect(It.IsAny<IPEndPoint>())).Pass();
            
            
            // setup UUT
            var connectionManager = new ConnectionManager(receiveDataProcessor.Object, binaryDataProcessor.Object);
            
            connectionManager.SetAlternativeTcpSocketForTesting(tcpSocket.Object);
            connectionManager.Connect(It.IsAny<IPEndPoint>());
            
            // verify connection manager is in connected state
            Assert.True(connectionManager.Connected);
            Assert.NotNull(connectionManager.ClientSession);
        }


        [Fact]
        public void Disconnect_SocketIsClosed()
        {
            var clientSession = new Mock<IClientSession>();
            var receiveDataProcessor = new Mock<IReceiveDataProcessor>();
            var binaryDataProcessor = new Mock<IBinaryDataProcessor>();
            var tcpSocket = new Mock<ITcpSocket>();
            
            
            // setup UUT
            var connectionManager = new ConnectionManager(receiveDataProcessor.Object, binaryDataProcessor.Object);
            
            connectionManager.SetAlternativeTcpSocketForTesting(tcpSocket.Object);
            connectionManager.Connect(It.IsAny<IPEndPoint>());
            connectionManager.Disconnect();
            
            tcpSocket.Verify(t => t.Disconnect(It.IsAny<bool>()));
            clientSession.Verify(cs => cs.Close());
        }
    }
}