using MessageBroker.SocketServer.Models;
using MessageBroker.SocketServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Service
{
    public class SocketServerOrchestrator
    {
        private readonly ISocketServer _server;
        private readonly IMessageQueue _messageQueue;
        private readonly Action<SocketClient> _onClientConnectedEvent;
        private readonly Action<SocketClient> _onClientDicconnectedEvent;

        public SocketServerOrchestrator(ISocketServer server, IMessageQueue messageQueue, Action<SocketClient> onClientConnectedEvent, Action<SocketClient> onClientDisconnectedEvent)
        {
            _server = server;
            _messageQueue = messageQueue;
            _onClientConnectedEvent = onClientConnectedEvent;
            _onClientDicconnectedEvent = onClientDisconnectedEvent;

            Setup();
        }

        private void Setup()
        {
            _server.OnClientConnected += _onClientConnectedEvent;
            _server.OnClientDisconnected += _onClientDicconnectedEvent;
        }

        public void Start()
        {
            _server.Start();
        }

        public void Stop()
        {
            _server.Stop();
        }


    }
}
