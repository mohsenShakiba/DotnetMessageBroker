using System;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Client.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Client.SocketClient
{
    public interface ISocketClient
    {
        ChannelWriter<SendData> SendDataChannel { get; }
        void Connect(SocketConnectionConfiguration configuration);
        Task<SendAsyncResult> SendAsync(Guid id, Memory<byte> data, bool completeOnAcknowledge);
    }
}