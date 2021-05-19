using System;
using System.Buffers;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Represents a data sent from publisher
    /// </summary>
    public struct Message
    {
        public Guid Id { get; set; }
        public string Route { get; set; }
        public Memory<byte> Data { get; set; }
        public byte[] OriginalMessageData { get; set; }

        public TopicMessage ToTopicMessage(string queueName)
        {
            var newData = ArrayPool<byte>.Shared.Rent(Data.Length);
            Data.CopyTo(newData);
            return new TopicMessage
            {
                Id = Guid.NewGuid(),
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