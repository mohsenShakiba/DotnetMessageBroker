using MessageBroker.Core;
using MessageBroker.SocketServer.Server;
using System.Net;

namespace MessageBroker.SocketServer.Service
{
    public class SocketServerBuilder
    {
        private IPEndPoint _endPoint;
        private ISocketServer _socketServer;
        private IMessageProcessor _messageProcessor;

        public SocketServerBuilder WithEndPoint(IPEndPoint endpoint)
        {
            _endPoint = endpoint;
            return this;
        }

        public SocketServerBuilder WithProcessor(IMessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor;
            return this;
        }

        public ISocketServer Build()
        {
            return new TcpSocketServer(_endPoint, _messageProcessor);
        }
    }
}
