using System;
using System.Net.Sockets;
using MessageBroker.Client.Models;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client.ConnectionManager
{
    public class ConnectionManager : IConnectionManager
    {
        public bool IsConnected => _socket.Connected;
        public string LastSocketError => _lastSocketError;

        public Socket Socket => _socket;

        private SocketConnectionConfiguration _configuration;
        private string _lastSocketError;
        
        private readonly object o = new();
        private readonly ILogger<ConnectionManager> _logger;
        private readonly Socket _socket;
        

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect(SocketConnectionConfiguration configuration)
        {
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;

            TryConnect();
        }

        public void CheckConnectionStatusAndRetryIfDisconnected()
        {
            if (_socket.Connected)
            {
                _logger.LogWarning("socket, already connected");
                return;
            }

            if (!_configuration.RetryOnFailure)
            {
                _logger.LogWarning("connection failed");
                return;
            }

            _logger.LogWarning("attempting to reconnect to endpoint");

            TryConnect();
        }

        private void TryConnect()
        {
            while (true)
            {
                try
                {
                    if (_socket.Connected)
                    {
                        _logger.LogWarning("failed to reconnect, socket already connected");
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
                }
            }
        }

        private void OnConnectionFailed(SocketException e)
        {
            _lastSocketError = e.Message;
            _logger.LogError($"failed to connect to endpoint, socket error: {e}");
        }

        private void OnConnected()
        {
            _logger.LogInformation("socket successfully connected to endpoint");
        }


        public void Disconnect()
        {
            _socket.Disconnect(true);
        }
    }
}