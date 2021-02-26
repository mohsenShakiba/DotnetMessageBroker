using MessageBroker.TCP;
using MessageBroker.TCP.Client;

namespace MessageBroker.Client.ConnectionManagement
{
    public interface IConnectionManager : ISocketEventProcessor
    {
        public IClientSession ClientSession { get; }
        public bool Connected { get; }

        void Connect(SocketConnectionConfiguration configuration, bool debug);
        void Disconnect();
    }
}