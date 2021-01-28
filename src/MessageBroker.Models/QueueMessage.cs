using System;

namespace MessageBroker.Models
{
    public ref struct QueueMessage
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
        public string Route { get; init; }
        public Memory<byte> Data { get; init; }
        public byte[] OriginalMessageData { get; init; }
    }
}