using System;
using System.Buffers;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Represents a message to be received by the client from topic
    /// </summary>
    public struct TopicMessage
    {
        public Guid Id { get; set; }
        public string TopicName { get; set; }
        public string Route { get; set; }
        public Memory<byte> Data { get; set; }
        public byte[] OriginalMessageData { get; set; }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(OriginalMessageData);
        }
    }
}