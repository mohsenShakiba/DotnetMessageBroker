using System;
using System.Threading.Tasks;
using MessageBroker.TCP;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.SocketWrapper;

namespace Tests.Classes
{
    public class TestClientSession: IClientSession
    {
        private bool _return;

        public TestClientSession(bool @return)
        {
            _return = @return;
        }

        public Guid Id { get; }
        public bool Debug { get; set; }
        
        public void Use(ITcpSocket socket)
        {
        }

        public void ForwardEventsTo(ISocketEventProcessor socketEventProcessor)
        {
        }

        public void ForwardDataTo(ISocketDataProcessor socketDataProcessor)
        {
        }

        public Task<bool> SendAsync(Memory<byte> payload)
        {
            return Task.FromResult(_return);
        }

        public void Close()
        {
        }
    }
}