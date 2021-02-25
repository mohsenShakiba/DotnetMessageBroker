using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessageBroker.TCP;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.SocketWrapper;

namespace Benchmarks
{
    public class TestClientSession : IClientSession
    {
        public Guid Id { get; set; }

        public void Use(Socket socket)
        {
            // do nothing
        }

        public void Use(ITcpSocket socket)
        {
            throw new NotImplementedException();
        }

        public void ForwardEventsTo(ISocketEventProcessor socketEventProcessor)
        {
            // do nothing
        }

        public void ForwardDataTo(ISocketDataProcessor socketDataProcessor)
        {
            // do nothing
        }

        public Task<bool> SendAsync(Memory<byte> payload)
        {
            return Task.FromResult(true);
        }

        public void Close()
        {
            // do nothing
        }
    }
}