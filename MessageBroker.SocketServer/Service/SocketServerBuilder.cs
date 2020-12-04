using MessageBroker.SocketServer.Models;
using MessageBroker.SocketServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Service
{
    public class SocketServerBuilder
    {

        private ISocketServer _socketServer;
        private InMemoryMessageQueue _memoryMessageQueue;
        private Action<SocketClient> _onClientConnectedEvent;
        private Action<SocketClient> _onClientDisconnectedEvent;

        public static SocketServerBuilder CreateTcpSocket(IPEndPoint endpoint)
        {
            return new SocketServerBuilder { _socketServer = new TcpSocketServer(endpoint) };
        }

        public SocketServerBuilder WithInMemoryMessageQueue()
        {
            _memoryMessageQueue = new InMemoryMessageQueue();
            return this;
        }

        public SocketServerBuilder WithEvents(Action<SocketClient> onClientConnectedEvent, Action<SocketClient> onClientDisconnectedEvent)
        {
            _onClientConnectedEvent = onClientConnectedEvent;
            _onClientDisconnectedEvent = onClientDisconnectedEvent;
            return this;
        }

        public SocketServerOrchestrator Build()
        {
            return new SocketServerOrchestrator(_socketServer, _memoryMessageQueue, _onClientConnectedEvent, _onClientDisconnectedEvent);
        }
    }
}
