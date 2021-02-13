using System;
using System.Buffers;

namespace MessageBroker.Client.Models
{
    public class ReceiveData : IDisposable
    {
        public Memory<byte> Data { get; init; }
        public byte[] Buffer { get; init; }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}