using System;
using System.Buffers;

namespace MessageBroker.Models
{
    /// <summary>
    /// Represents a message to be received by the client from topic
    /// </summary>
    public struct TopicMessage
    {
        public Guid Id { get; init; }
        public string TopicName { get; init; }
        public string Route { get; init; }
        public Memory<byte> Data { get; init; }
        public byte[] OriginalMessageData { get; init; }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(OriginalMessageData);
        }
    }

}