using System;
using System.Threading.Tasks;
using MessageBroker.Core.Socket.Client;

namespace Benchmarks
{
    public class TestClientSession : IClientSession
    {
        public Guid Id { get; set; }
        
        public void SetupSendCompletedHandler(Action<Guid> onSendCompleted, Action<Guid> onMessageError)
        {
            // do nothing
        }

        public void SetSendPayloadId(Guid sendPayloadId)
        {
            // do nothing
        }


        public void Send(Memory<byte> payload)
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

        public void Dispose()
        {
            // do nothing
        }
    }
}