using System;
using MessageBroker.SocketServer.Abstractions;

namespace Benchmarks
{
    public class TestClientSession : IClientSession
    {
        public Guid SessionId { get; set; }
        
        public void SetupSendCompletedHandler(Action onSendCompleted)
        {
        }

        public void Send(Memory<byte> payload)
        {
            // do nothing
        }

        public bool SendAsync(Memory<byte> payload)
        {
            return false;
        }

        public void Close()
        {
            // do nothing
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}