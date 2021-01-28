﻿using System;
using System.Net;
using System.Net.Sockets;
using MessageBroker.Common.Logging;
using MessageBroker.SocketServer.Abstractions;
using Microsoft.Extensions.Logging;

namespace MessageBroker.SocketServer
{
    /// <summary>
    ///     TCP implementation of ISocketServer
    /// </summary>
    public class TcpSocketServer : ISocketServer, ISessionEventListener
    {
        private readonly ISessionResolver _sessionResolver;
        private readonly ISocketEventProcessor _socketEventProcessor;
        private IPEndPoint _endPoint;
        private bool _isAccepting;

        private Socket _socket;
        private SocketAsyncEventArgs _socketAsyncEventArgs;

        public TcpSocketServer(ISocketEventProcessor socketEventProcessor, ISessionResolver sessionResolver)
        {
            _socketEventProcessor = socketEventProcessor;
            _sessionResolver = sessionResolver;
        }


        /// <summary>
        ///     this method will be called by the session when a new message has been received
        /// </summary>
        /// <param name="sessionId">
        ///     the id of session
        /// </param>
        /// <param name="data">
        ///     the message received
        /// </param>
        public void OnReceived(Guid sessionId, Memory<byte> data)
        {
            _socketEventProcessor.DataReceived(sessionId, data);
        }

        /// <summary>
        ///     this method will be called by the session when client has been forcibly disconnected
        /// </summary>
        /// <param name="SessionId">
        ///     the id of session
        /// </param>
        public void OnSessionDisconnected(Guid SessionId)
        {
            _sessionResolver.Remove(SessionId);
            _socketEventProcessor.ClientDisconnected(SessionId);
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

            RemoveAllSessions();
        }

        /// <summary>
        ///     this method is called when a new socket has been soccessfully accepted
        /// </summary>
        private void CreateSocket()
        {
            _socketAsyncEventArgs = new SocketAsyncEventArgs();
            _socketAsyncEventArgs.Completed += OnAcceptCompleted;

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
        private void OnAcceptSuccess(Socket socket)
        {
            Logger.LogInformation("accepted new socket connection");
            
            var session = new ClientSession(this, socket);

            _sessionResolver.Add(session);

            _socketEventProcessor.ClientConnected(session.SessionId);
        }

        /// <summary>
        /// called when accepting socket fails
        /// </summary>
        /// <param name="err"></param>
        private void OnAcceptError(SocketError err)
        {
            Logger.LogError($"failed to accept socket connection, error: {err}");
        }

        /// <summary>
        /// called by Stop method to remove all sessions
        /// </summary>
        private void RemoveAllSessions()
        {
            Logger.LogInformation("removing all sessions");
            foreach (var session in _sessionResolver.Sessions)
            {
                _socketEventProcessor.ClientDisconnected(session.SessionId);
                session.Close();
            }
        }
    }
}