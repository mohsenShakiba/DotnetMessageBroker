using MessageBroker.SocketServer.Models;
using MessageBroker.SocketServer.Service;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public class TcpSocketServer : TcpServer, ISocketServer
    {
        public event Action<SocketClient> OnClientConnected;
        public event Action<SocketClient> OnClientDisconnected;
        private readonly IMessageQueue _messageQueue;

        public TcpSocketServer(IPEndPoint endpoint, IMessageQueue messageQueue) : base(endpoint)
        {
            _messageQueue = messageQueue;
        }

        public void Start() => base.Start();

        public void Stop() => base.Stop();

        protected override TcpSession CreateSession()
        {
            return new ClientSession(this, OnMessageReceived);
        }

        protected override void OnConnected(TcpSession session)
        {
            OnClientConnected?.Invoke(new SocketClient(session.Id));
        }

        protected override void OnDisconnected(TcpSession session)
        {
            OnClientConnected?.Invoke(new SocketClient(session.Id));
        }


        protected void OnMessageReceived(MessagePayload msg)
        {
            _messageQueue.Push(msg);
        }

    }
}
