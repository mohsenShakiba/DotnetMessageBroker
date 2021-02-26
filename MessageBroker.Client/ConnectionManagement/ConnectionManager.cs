using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Logging;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.SocketWrapper;

namespace MessageBroker.Client.ConnectionManagement
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly IClientSession _clientSession;
        private readonly IReceiveDataProcessor _receiveDataProcessor;

        private SocketConnectionConfiguration _configuration;
        private ITcpSocket _tcpSocket;
        private bool _closed;
        private bool _connectionReady;
        private bool _debug;

        public IClientSession ClientSession
        {
            get
            {
                if (_closed)
                    throw new Exception("The connection manager has been closed");

                if (!_connectionReady)
                    TryConnect();

                return _clientSession;
            }
        }

        public bool Connected => _connectionReady;


        public ConnectionManager(IClientSession clientSession, IReceiveDataProcessor receiveDataProcessor)
        {
            _clientSession = clientSession;
            _receiveDataProcessor = receiveDataProcessor;
            
            SetDefaultTcpSocket();
        }

        private void SetDefaultTcpSocket()
        {
            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _tcpSocket = new TcpSocket(tcpSocket);
        }

        public void SetAlternativeTcpSocketForTesting(ITcpSocket tcpSocket)
        {
            _tcpSocket = tcpSocket;
        }

        public void Connect(SocketConnectionConfiguration configuration, bool debug = false)
        {
            _debug = debug;
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;

            TryConnect();
        }

        public void Reconnect()
        {
            if (_tcpSocket.Connected)
            {
                Logger.LogWarning("socket, already connected");
                return;
            }

            if (!_configuration.RetryOnFailure)
            {
                Logger.LogWarning("connection failed");
                return;
            }

            Logger.LogWarning("attempting to reconnect to endpoint");

            _closed = false;
            _connectionReady = false;

            TryConnect();
        }

        public void Disconnect()
        {
            _tcpSocket.Disconnect(true);
            _clientSession.Close();
            _closed = true;
        }

        public void ClientDisconnected(IClientSession clientSession)
        {
            if (_closed)
                return;

            TryConnect();
        }

        public void ClientConnected(IClientSession clientSession)
        {
            // do nothing
        }

        private void TryConnect()
        {
            while (true)
                try
                {
                    if (_closed)
                        throw new Exception("ConnectionManager is closed");

                    if (_tcpSocket.Connected)
                    {
                        Logger.LogWarning("failed to reconnect, socket already connected");
                        return;
                    }

                    _tcpSocket.Connect(_configuration.IpEndPoint);

                    OnConnected();

                    break;
                }
                catch (SocketException e)
                {
                    OnConnectionFailed(e);

                    if (!_configuration.RetryOnFailure)
                        break;

                    Thread.Sleep(100);

                    throw;
                }
        }

        private void OnConnectionFailed(SocketException e)
        {
            Logger.LogError($"failed to connect to endpoint, socket error: {e}");

            _connectionReady = false;
        }

        private void OnConnected()
        {
            Logger.LogInformation("socket successfully connected to endpoint");
            _clientSession.Debug = _debug;
            _clientSession.ForwardEventsTo(this);
            _clientSession.ForwardDataTo(_receiveDataProcessor);
            _clientSession.Use(_tcpSocket);

            _connectionReady = true;
        }
    }
}