using System;
using System.Net;
using MessageBroker.TCP;
using MessageBroker.TCP.Client;

namespace MessageBroker.Client.ConnectionManagement
{
    public interface IConnectionManager : ISocketEventProcessor
    {
        public IClientSession ClientSession { get; }
        public bool Connected { get; }
        void Connect(IPEndPoint ipEndPoint);
        void Disconnect();
        void SimulateInterrupt();
    }
}