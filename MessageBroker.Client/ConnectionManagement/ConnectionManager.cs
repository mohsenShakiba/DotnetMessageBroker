using System;
using System.Net.Sockets;
using System.Threading;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.SocketWrapper;

namespace MessageBroker.Client.ConnectionManagement
{
    public class ConnectionManager : IConnectionManager
    {
        private IClientSession _clientSession;
        private readonly IReceiveDataProcessor _receiveDataProcessor;

        private SocketConnectionConfiguration _configuration;
        private ITcpSocket _tcpSocket;
        private bool _closed;
        private bool _connectionReady;
        private bool _reconnecting;
        private object _lock;

        public IClientSession ClientSession
        {
            get
            {
                if (_closed)
                    throw new Exception("The connection manager has been closed");

                while (true)
                {
                    if (_connectionReady)
                        return _clientSession;
                    
                    Thread.Sleep(1000);
                    Console.WriteLine("sleeping");
                }
            }
        }

        public event Action OnClientConnected;

        public bool Connected => _connectionReady;


        public ConnectionManager(IClientSession clientSession, IReceiveDataProcessor receiveDataProcessor)
        {
            _clientSession = clientSession;
            _receiveDataProcessor = receiveDataProcessor;
            _lock = new();
            SetDefaultTcpSocket();
            
            Console.WriteLine($"connection manager -> creating client session with id {_clientSession.Id}");
        }

        private void SetDefaultTcpSocket()
        {
            _tcpSocket = new TcpSocket();
        }

        public void SetAlternativeTcpSocketForTesting(ITcpSocket tcpSocket)
        {
            _tcpSocket = tcpSocket;
        }

        public void Connect(SocketConnectionConfiguration configuration)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;

            TryConnect();
        }

        public void Disconnect()
        {
            _tcpSocket.Disconnect(true);
            _clientSession.Close();
            _closed = true;
        }

        public void SimulateConnectionDisconnection()
        {
            _tcpSocket.Disconnect(true);
        }

        public void ClientDisconnected(IClientSession clientSession)
        {
            Logger.LogWarning($"client disconnected called {clientSession.Id}");
            // do nothing
            if (_closed)
                return;

            lock (_lock)
            {
                Logger.LogWarning($"client disconnected called after {_reconnecting}");
                if (_reconnecting)
                    return;

                _reconnecting = true;
            }

            TryConnect();
        }

        public void ClientConnected(IClientSession clientSession)
        {
            // do nothing
        }

        private void TryConnect()
        {
            Logger.LogWarning($"Try connect called {_clientSession.Id}");
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

                    if (_tcpSocket.Connected)
                    {
                        OnConnected();
                        break;
                    }

                }
                catch (SocketException e)
                {
                    OnConnectionFailed(e);

                    if (!_configuration.RetryOnFailure)
                        break;

                    Thread.Sleep(100);
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
            // todo: fix
            var binaryDataProcessor = new BinaryDataProcessor();
            _clientSession = new ClientSession(binaryDataProcessor);
            _clientSession.ForwardEventsTo(this);
            _clientSession.ForwardDataTo(_receiveDataProcessor);
            var isConnected = _tcpSocket.Connected;
            _clientSession.Use(_tcpSocket);
            Logger.LogWarning($"calling use for {_clientSession.Id}");

            lock (_lock)
            {
                _reconnecting = false;
            }

            _connectionReady = true;
            
            OnClientConnected?.Invoke();

        }
    }
}