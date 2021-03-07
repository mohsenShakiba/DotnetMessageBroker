using System;
using System.Net;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Binary;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.SocketWrapper;

namespace MessageBroker.Client.ConnectionManagement
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly IReceiveDataProcessor _receiveDataProcessor;
        private readonly IBinaryDataProcessor _binaryDataProcessor;

        private IClientSession _clientSession;
        private ITcpSocket _tcpSocket;
        
        public IClientSession ClientSession => _clientSession;
        public bool Connected => _tcpSocket.Connected;


        public ConnectionManager(IReceiveDataProcessor receiveDataProcessor, IBinaryDataProcessor binaryDataProcessor)
        {
            _receiveDataProcessor = receiveDataProcessor;
            _binaryDataProcessor = binaryDataProcessor;
            SetDefaultTcpSocket();
        }

        private void SetDefaultTcpSocket()
        {
            _tcpSocket = new TcpSocket();
        }

        public void SetAlternativeTcpSocketForTesting(ITcpSocket tcpSocket)
        {
            _tcpSocket = tcpSocket;
        }

        public void Connect(IPEndPoint ipEndPoint)
        {
            _ = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));

            _tcpSocket.Connect(ipEndPoint);
            
            _clientSession = new ClientSession(_binaryDataProcessor);
            
            _clientSession.ForwardEventsTo(this);
            _clientSession.ForwardDataTo(_receiveDataProcessor);
            _clientSession.Use(_tcpSocket);
        }

        public void Disconnect()
        {
            _tcpSocket.Disconnect(true);
            _clientSession.Close();
        }

        public void SimulateInterrupt()
        {
            _tcpSocket.Disconnect(true);
        }

        public void ClientDisconnected(IClientSession clientSession)
        {
            // do nothing
        }

        public void ClientConnected(IClientSession clientSession)
        {
            // do nothing
        }
    }
}