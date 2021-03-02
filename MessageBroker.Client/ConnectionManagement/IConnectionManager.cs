using System;
using MessageBroker.TCP;
using MessageBroker.TCP.Client;

namespace MessageBroker.Client.ConnectionManagement
{
    public interface IConnectionManager : ISocketEventProcessor
    {
        public IClientSession ClientSession { get; }
        public event Action OnClientConnected;
        public bool Connected { get; }
        void Connect(SocketConnectionConfiguration configuration);
        void Disconnect();
        void SimulateConnectionDisconnection();
    }
}