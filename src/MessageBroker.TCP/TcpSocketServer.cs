using System;
using System.Net;
using System.Net.Sockets;
using MessageBroker.Common.Logging;
using MessageBroker.TCP.EventArgs;

namespace MessageBroker.TCP
{
    /// <inheritdoc />
    public class TcpSocketServer : ISocketServer
    {
        private readonly IPEndPoint _endPoint;

        private bool _isDisposed;
        private bool _isAccepting;

        /// <summary>
        /// Socket object used for listening to IPEndPoint
        /// </summary>
        private Socket _socket;
        
        /// <summary>
        /// SocketAsyncEventArgs used for accepting connections
        /// </summary>
        private SocketAsyncEventArgs _socketAsyncEventArgs;
        
        public event EventHandler<SocketAcceptedEventArgs> OnSocketAccepted;
        
        public TcpSocketServer(ConnectionProvider connectionProvider)
        {
            _endPoint = connectionProvider.IpEndPoint;
        }

        public TcpSocketServer(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

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
            _socket.Listen();

            Logger.LogInformation($"started socket on endpoint {_endPoint}");

            BeginAcceptConnection();
        }

        public void Stop()
        {
            ThrowIfDisposed();

            Logger.LogInformation("stopping socket server");
            
            _isAccepting = false;

            _socketAsyncEventArgs.Completed -= OnAcceptCompleted;

            _socket.Close();
            _socket.Dispose();

            _socketAsyncEventArgs.Dispose();
            
            Dispose();
        }

        /// <summary>
        /// Start accepting connections until server is stopped
        /// </summary>
        /// <remarks>if accepting encounters an error then the server is stopped</remarks>
        private void BeginAcceptConnection()
        {
            try
            {
                ResetAcceptEventArgs();
                
                // accept while sync, break when we go async\
                while (_isAccepting && !_socket.AcceptAsync(_socketAsyncEventArgs))
                {
                    OnAcceptCompleted(null, _socketAsyncEventArgs);
                    ResetAcceptEventArgs();
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Server encountered an exception while trying to accept connection, exception: {e}");
                Stop();
            }
        }

        private void ResetAcceptEventArgs()
        {
            _socketAsyncEventArgs.AcceptSocket = null;
            _socketAsyncEventArgs.UserToken = this;
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

            var server = (TcpSocketServer)socketAsyncEventArgs.UserToken;
            server?.BeginAcceptConnection();
        }

        private void OnAcceptSuccess(Socket socket)
        {
            Logger.LogInformation($"accepted new socket connection from {socket.RemoteEndPoint}");

            var tcpSocket = new TcpSocket(socket);

            var socketAcceptedEventArgs = new SocketAcceptedEventArgs {Socket = tcpSocket};
            
            OnSocketAccepted?.Invoke(this, socketAcceptedEventArgs);
        }

        private void OnAcceptError(SocketError err)
        {
            Logger.LogError($"failed to accept socket connection, error: {err}");
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException($"Server has been disposed");
        }

        /// <summary>
        /// Will mark the object as disposed
        /// <remarks>Calling Dispose will not stop the server, Stop must be called</remarks>
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}