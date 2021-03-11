using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.TCP;
using MessageBroker.TCP.Client;

namespace MessageBroker.Client.ConnectionManagement
{
    public interface IConnectionManager : ISocketEventProcessor
    {
        event Action OnConnected;
        event Action OnDisconnected;
        
        public IClientSession ClientSession { get; }
        public bool Connected { get; }
        void Connect(IPEndPoint ipEndPoint);
        void Reconnect();
        void Disconnect();
        void SimulateInterrupt();
        ValueTask WaitForReadyAsync(CancellationToken cancellationToken);
        void MarkAsReady();
    }
}