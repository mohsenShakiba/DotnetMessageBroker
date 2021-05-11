using System;
using System.Buffers;

namespace MessageBroker.Models
{
    /// <summary>
    /// Represents a data sent from publisher
    /// </summary>
    public struct Message
    {
        public Guid Id { get; init; }
        public string Route { get; init; }
        public Memory<byte> Data { get; init; }
        public byte[] OriginalMessageData { get; init; }

        public TopicMessage ToTopicMessage(string queueName)
        {
            var newData = ArrayPool<byte>.Shared.Rent(Data.Length);
            Data.CopyTo(newData);
            // todo: use new guid
            return new TopicMessage
            {
                Id = Id,
                Data = newData.AsMemory(0, Data.Length),
                Route = Route,
                TopicName = queueName,
                OriginalMessageData = newData
            };
        }

        public void Dispose()
        {
            if (OriginalMessageData is not null)
                ArrayPool<byte>.Shared.Return(OriginalMessageData);
        }
    }
}