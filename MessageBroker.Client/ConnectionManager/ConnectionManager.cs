using System;
using System.Net.Sockets;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Logging;
using MessageBroker.Socket.Client;

namespace MessageBroker.Client.ConnectionManager
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly IClientSession _clientSession;
        private readonly IReceiveDataProcessor _receiveDataProcessor;

        private readonly System.Net.Sockets.Socket _socket;
        private bool _closed;

        private SocketConnectionConfiguration _configuration;
        private bool _connectionReady;


        public ConnectionManager(IClientSession clientSession, IReceiveDataProcessor receiveDataProcessor)
        {
            _socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSession = clientSession;
            _receiveDataProcessor = receiveDataProcessor;
        }

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

        public void Connect(SocketConnectionConfiguration configuration)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;

            TryConnect();
        }

        public void Reconnect()
        {
            if (_socket.Connected)
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
            _socket.Disconnect(true);
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

                    if (_socket.Connected)
                    {
                        Logger.LogWarning("failed to reconnect, socket already connected");
                        return;
                    }

                    _socket.Connect(_configuration.IpEndPoint);

                    OnConnected();

                    break;
                }
                catch (SocketException e)
                {
                    OnConnectionFailed(e);

                    if (!_configuration.RetryOnFailure)
                        break;

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

            _clientSession.ForwardEventsTo(this);
            _clientSession.ForwardDataTo(_receiveDataProcessor);

            _clientSession.Use(_socket);

            _connectionReady = true;
        }
    }
}