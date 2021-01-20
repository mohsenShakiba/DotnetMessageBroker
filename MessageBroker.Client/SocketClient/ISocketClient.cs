using System;
using System.Net;
using System.Threading.Tasks;

namespace MessageBroker.Client.SocketClient
{
    public interface ISocketClient
    {
        void Connect(IPEndPoint endPoint, bool retryOnFailure);
        Task<bool> SendAsync(Guid payloadId, Memory<byte> payload, bool completeOnAcknowledge);

        Task<Memory<byte>> ReceiveAsync();
    }
}