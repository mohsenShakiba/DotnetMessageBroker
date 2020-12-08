using MessageBroker.Core;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Models;
using NetCoreServer;
using System.Net;

namespace MessageBroker.SocketServer.Server
{
    public class TcpSocketServer : TcpServer, ISocketServer
    {
        private readonly IMessageProcessor _messageProcessor;

        public TcpSocketServer(IPEndPoint endpoint, IMessageProcessor messageProcessor) : base(endpoint)
        {
            _messageProcessor = messageProcessor;
        }

        public void Start() => base.Start();

        public void Stop() => base.Stop();

        protected override TcpSession CreateSession()
        {
            return new ClientSession(this, OnMessageReceived);
        }

        protected override void OnConnected(TcpSession session)
        {
            _messageProcessor.OnClientConnected(session.Id);
        }

        protected override void OnDisconnected(TcpSession session)
        {
            _messageProcessor.OnClientDisconnected(session.Id);
        }


        protected void OnMessageReceived(Payload msg)
        {
            _messageProcessor.OnMessage(msg);
        }

    }
}
