using System;
using System.Buffers;

namespace MessageBroker.Models
{
    public ref struct QueueMessage
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
        public string Route { get; init; }
        public Memory<byte> Data { get; init; }
        public byte[] OriginalMessageData { get; init; }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(OriginalMessageData);
        }
    }
}