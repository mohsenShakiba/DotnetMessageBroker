using System;
using System.Net;
using System.Net.Sockets;
using MessageBroker.Common.Tcp.EventArgs;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Common.Tcp
{
    /// <inheritdoc />
    public sealed class TcpListener : IListener
    {
        private readonly IPEndPoint _endPoint;
        private readonly ILogger<TcpListener> _logger;
        private bool _isAccepting;
        private bool _isDisposed;

        /// <summary>
        /// Socket object used for listening to IPEndPoint
        /// </summary>
        private Socket _socket;

        /// <summary>
        /// SocketAsyncEventArgs used for accepting connections
        /// </summary>
        private SocketAsyncEventArgs _socketAsyncEventArgs;

        public TcpListener(ConnectionProvider connectionProvider, ILogger<TcpListener> logger)
        {
            _endPoint = connectionProvider.IpEndPoint;
            _logger = logger;
        }

        public TcpListener(IPEndPoint endPoint, ILogger<TcpListener> logger)
        {
            _endPoint = endPoint;
            _logger = logger;
        }

        public event EventHandler<SocketAcceptedEventArgs> OnSocketAccepted;

        public void Start()
        {
            ThrowIfDisposed();

            if (_isAccepting)
                throw new InvalidOperationException("Server is already accepting connection");

            _isAccepting = true;

            _socketAsyncEventArgs = new SocketAsyncEventArgs();
            _socketAsyncEventArgs.Completed += OnAcceptCompleted;

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_endPoint);
            _socket.Listen(1024);

            _logger.LogInformation($"Started socket on endpoint {_endPoint}");

            BeginAcceptConnection();
        }

        public void Stop()
        {
            ThrowIfDisposed();

            _logger.LogInformation("Stopping socket server");

            _isAccepting = false;

            _socketAsyncEventArgs.Completed -= OnAcceptCompleted;

            _socket.Close();
            _socketAsyncEventArgs.Dispose();

            Dispose();
        }

        /// <summary>
        /// Will mark the object as disposed
        /// <remarks>Calling Dispose will not stop the server, Stop must be called</remarks>
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        /// <summary>
        /// Start accepting connections until server is stopped
        /// </summary>
        /// <remarks>if accepting encounters an error then the server is stopped</remarks>
        private void BeginAcceptConnection()
        {
            try
            {
                _socketAsyncEventArgs.AcceptSocket = null;

                // accept while sync, break when we go async
                while (_isAccepting && !_socket.AcceptAsync(_socketAsyncEventArgs))
                {
                    OnAcceptCompleted(null, _socketAsyncEventArgs);
                    _socketAsyncEventArgs.AcceptSocket = null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Server encountered an exception while trying to accept connection, exception: {e}");
            }
        }

        private void OnAcceptCompleted(object _, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            switch (socketAsyncEventArgs.SocketError)
            {
                case SocketError.Success:
                    OnAcceptSuccess(socketAsyncEventArgs.AcceptSocket);
                    break;
                default:
                    OnAcceptError(socketAsyncEventArgs.SocketError);
                    break;
            }

            BeginAcceptConnection();
        }

        private void OnAcceptSuccess(Socket socket)
        {
            _logger.LogInformation($"Accepted new socket connection from {socket.RemoteEndPoint}");

            var tcpSocket = new TcpSocket(socket);

            var socketAcceptedEventArgs = new SocketAcceptedEventArgs {Socket = tcpSocket};

            OnSocketAccepted?.Invoke(this, socketAcceptedEventArgs);
        }

        private void OnAcceptError(SocketError err)
        {
            _logger.LogError($"Failed to accept socket connection, error: {err}");
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Server has been disposed");
        }
    }
}