using MessageBroker.Socket;
using MessageBroker.Socket.Client;

namespace MessageBroker.Client.ConnectionManager
{
    public interface IConnectionManager : ISocketEventProcessor
    {
        public IClientSession ClientSession { get; }

        void Connect(SocketConnectionConfiguration configuration);
        void Reconnect();
        void Disconnect();
    }
}