using System;
using System.Net;
using System.Net.Sockets;
using MessageBroker.Common.Logging;
using MessageBroker.Socket.Client;
using MessageBroker.Socket.SocketWrapper;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Socket.Server
{
    /// <summary>
    ///     TCP implementation of ISocketServer
    /// </summary>
    public class TcpSocketServer : ISocketServer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISocketDataProcessor _socketDataProcessor;
        private readonly ISocketEventProcessor _socketEventProcessor;
        private IPEndPoint _endPoint;
        private bool _isAccepting;

        private System.Net.Sockets.Socket _socket;
        private SocketAsyncEventArgs _socketAsyncEventArgs;

        public TcpSocketServer(ISocketEventProcessor socketEventProcessor, ISocketDataProcessor socketDataProcessor,
            IServiceProvider serviceProvider)
        {
            _socketEventProcessor = socketEventProcessor;
            _socketDataProcessor = socketDataProcessor;
            _serviceProvider = serviceProvider;
        }


        /// <summary>
        ///     This method will start the server and begin accepting connections
        /// </summary>
        /// <param name="endpoint">
        ///     the endpoint server will listen on
        /// </param>
        public void Start(IPEndPoint endpoint)
        {
            _endPoint = endpoint;
            _isAccepting = true;
            CreateSocket();
        }

        /// <summary>
        ///     will stop and dispose the server,
        ///     all the sessions will be disconnected and removed
        /// </summary>
        public void Stop()
        {
            Logger.LogInformation("stopping socket server");

            _isAccepting = false;

            _socketAsyncEventArgs.Completed -= OnAcceptCompleted;

            _socket.Close();
            _socket.Dispose();

            _socketAsyncEventArgs.Dispose();
        }

        /// <summary>
        ///     this method is called when a new socket has been successfully accepted
        /// </summary>
        private void CreateSocket()
        {
            _socketAsyncEventArgs = new SocketAsyncEventArgs();
            _socketAsyncEventArgs.Completed += OnAcceptCompleted;

            _socket = new System.Net.Sockets.Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_endPoint);
            _socket.Listen();

            Logger.LogInformation("started socket connection");

            AcceptConnection();
        }

        /// <summary>
        ///     begin accepting a connection
        /// </summary>
        private void AcceptConnection()
        {
            if (!_isAccepting)
                return;

            _socketAsyncEventArgs.AcceptSocket = null;

            if (!_socket.AcceptAsync(_socketAsyncEventArgs))
                OnAcceptCompleted(null, _socketAsyncEventArgs);
        }

        /// <summary>
        ///     called by AcceptConnection when accepting compelets
        /// </summary>
        /// <param name="_"></param>
        /// <param name="socketAsyncEventArgs">SocketAsyncEventArgs</param>
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

            AcceptConnection();
        }

        /// <summary>
        ///     called when the socket can be accepted
        ///     this method will create a session
        /// </summary>
        /// <param name="socket"></param>
        private void OnAcceptSuccess(System.Net.Sockets.Socket socket)
        {
            Logger.LogInformation("accepted new socket connection");

            var client = _serviceProvider.GetRequiredService<IClientSession>();

            client.ForwardEventsTo(_socketEventProcessor);
            client.ForwardDataTo(_socketDataProcessor);

            var tcpSocket = new TcpSocket(socket);
            client.Use(tcpSocket);

            _socketEventProcessor.ClientConnected(client);
        }

        /// <summary>
        ///     called when accepting socket fails
        /// </summary>
        /// <param name="err"></param>
        private void OnAcceptError(SocketError err)
        {
            Logger.LogError($"failed to accept socket connection, error: {err}");
        }
    }
}