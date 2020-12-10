using MessageBroker.Core;
using MessageBroker.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MessageBroker.SocketServer.Server
{
    public class TcpSocketServer : ISocketServer
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly IPEndPoint _endPoint;
        private Socket _socket;
        private SocketAsyncEventArgs _socketAsyncEventArgs;
        private bool _isAccepting = false;
        private readonly List<ClientSession> _sessions = new();

        public TcpSocketServer(IPEndPoint endpoint, IMessageProcessor messageProcessor)
        {
            _endPoint = endpoint;
            _messageProcessor = messageProcessor;
        }

        public void Start()
        {
            _isAccepting = true;
            CreateSocket();
        }

        public void Stop()
        {
            _isAccepting = false;

            _socketAsyncEventArgs.Completed -= OnAcceptCompleted;

            _socket.Close();

            _socket.Dispose();

            _socketAsyncEventArgs.Dispose();

            RemoveAllSessions();
        }

        private void CreateSocket()
        {
            _socketAsyncEventArgs = new SocketAsyncEventArgs();
            _socketAsyncEventArgs.Completed += OnAcceptCompleted;

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_endPoint);
            _socket.Listen();

            AcceptConnection();
        }

        private void AcceptConnection()
        {
            if (!_isAccepting)
                return;

            _socketAsyncEventArgs.AcceptSocket = null;

            _socket.AcceptAsync(_socketAsyncEventArgs);
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

            AcceptConnection();
        }

        private void OnAcceptSuccess(Socket socket)
        {
            var session = new ClientSession(socket);

            _sessions.Add(session);
        }

        private void OnAcceptError(SocketError err)
        {
            Console.WriteLine($"failed to accept connection due to {err}");
        }


        private void RemoveAllSessions()
        {
            foreach (var session in _sessions)
                session.Close();
        }


        protected void OnMessageReceived(Payload msg)
        {
            _messageProcessor.OnMessage(msg);
        }

    }
}
