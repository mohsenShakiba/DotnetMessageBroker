using MessageBroker.Common;
using MessageBroker.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MessageBroker.SocketServer.Server
{
    /// <summary>
    /// TCP implementation of ISocketServer
    /// </summary>
    public class TcpSocketServer : ISocketServer
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly SessionConfiguration _sessionConfiguration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<TcpSocketServer> _logger;
        private readonly ConcurrentDictionary<Guid, ClientSession> _sessions;

        private Socket _socket;
        private SocketAsyncEventArgs _socketAsyncEventArgs;
        private bool _isAccepting;
        private IPEndPoint _endPoint;

        public TcpSocketServer(IMessageProcessor messageProcessor, SessionConfiguration sessionConfiguration, ILoggerFactory loggerFactory)
        {
            _messageProcessor = messageProcessor;
            _sessionConfiguration = sessionConfiguration;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<TcpSocketServer>();
            _sessions = new();
        }

        /// <summary>
        /// This method will start the server and begin accepting connections
        /// </summary>
        /// <param name="endpoint">
        /// the endpoint server will listen on
        /// </param>
        public void Start(IPEndPoint endpoint)
        {
            _endPoint = endpoint;
            _isAccepting = true;

            _logger.LogInformation($"Server now listening on {endpoint}");

            CreateSocket();
        }

        /// <summary>
        /// will stop and dispose the server,
        /// all the sessions will be disconnected and removed
        /// </summary>
        public void Stop()
        {
            _isAccepting = false;

            _socketAsyncEventArgs.Completed -= OnAcceptCompleted;

            _socket.Close();
            _socket.Dispose();

            _socketAsyncEventArgs.Dispose();

            RemoveAllSessions();
        }

        /// <summary>
        /// this method will be called by the session when a new message has been received
        /// </summary>
        /// <param name="sessionId">
        /// the id of session 
        /// </param>
        /// <param name="data">
        /// the message received 
        /// </param>
        internal void OnReceived(Guid sessionId, Memory<byte> data)
        {
            _messageProcessor.MessageReceived(sessionId, data);
        }

        /// <summary>
        /// this method will be called by the session when client has been forcibly disconnected
        /// </summary>
        /// <param name="SessionId">
        /// the id of session
        /// </param>
        internal void OnSessionDisconnected(Guid SessionId)
        {
            _logger.LogInformation($"removed session due to being disconnected, sessionId: {SessionId}");
            _sessions.TryRemove(SessionId, out _);
            _messageProcessor.ClientDisconnected(SessionId);
        }

        /// <summary>
        /// this method is called when a new socket has been soccessfully accepted
        /// </summary>
        private void CreateSocket()
        {
            _socketAsyncEventArgs = new SocketAsyncEventArgs();
            _socketAsyncEventArgs.Completed += OnAcceptCompleted;

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_endPoint);
            _socket.Listen();

            AcceptConnection();
        }

        /// <summary>
        /// begin accepting a connection
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
        /// called by AcceptConnection when accepting compelets 
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
        /// called when the socket can be accepted
        /// this method will create a session
        /// </summary>
        /// <param name="socket"></param>
        private void OnAcceptSuccess(Socket socket)
        {
            var sessionLogger = _loggerFactory.CreateLogger<ClientSession>();

            var session = new ClientSession(this, socket, _sessionConfiguration, sessionLogger);

            _logger.LogInformation($"accepted socket from {socket.RemoteEndPoint} with sessionId {session.SessionId}");

            _sessions[session.SessionId] = session;

            _messageProcessor.ClientConnected(session.SessionId);
        }

        /// <summary>
        /// called when accepting socket fails
        /// </summary>
        /// <param name="err"></param>
        private void OnAcceptError(SocketError err)
        {
            Console.WriteLine($"failed to accept connection due to {err}");
        }

        /// <summary>
        /// called by Stop method to remove all sessions
        /// </summary>
        private void RemoveAllSessions()
        {
            _logger.LogInformation("removing all sessions");
            foreach (var (sessionId, session) in _sessions)
            {
                _messageProcessor.ClientDisconnected(session.SessionId);
                session.Close();
            }
        }

        public void Send(Guid sessionId, byte[] payload)
        {
            if(_sessions.TryGetValue(sessionId, out var session))
            {
                session.Send(payload);
            }
            else
            {
                _logger.LogError($"session not found by id: {sessionId}");
            }
        }
    }
}
