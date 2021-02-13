using System;
using System.Threading.Tasks;

namespace MessageBroker.Socket.Client
{
    public interface IClientSession
    {
        Guid Id { get; }
        void Use(System.Net.Sockets.Socket socket);
        void ForwardEventsTo(ISocketEventProcessor socketEventProcessor);
        void ForwardDataTo(ISocketDataProcessor socketDataProcessor);
        Task<bool> SendAsync(Memory<byte> payload);
        void Close();
    }
}