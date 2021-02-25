using System;
using System.Threading.Tasks;
using MessageBroker.TCP.SocketWrapper;

namespace MessageBroker.TCP.Client
{
    public interface IClientSession
    {
        Guid Id { get; }
        void Use(ITcpSocket socket);
        void ForwardEventsTo(ISocketEventProcessor socketEventProcessor);
        void ForwardDataTo(ISocketDataProcessor socketDataProcessor);
        Task<bool> SendAsync(Memory<byte> payload);
        void Close();
    }
}