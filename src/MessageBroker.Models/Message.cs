using System;
using System.Buffers;

namespace MessageBroker.Models
{
    /// <summary>
    ///     the message contains data that is sent by publisher and received by subscriber
    /// </summary>
    public struct Message
    {
        public Guid Id { get; set; }
        public string Route { get; init; }
        public Memory<byte> Data { get; init; }
        public byte[] OriginalMessageData { get; init; }

        public QueueMessage ToQueueMessage(string queueName)
        {
            var newData = ArrayPool<byte>.Shared.Rent(Data.Length);
            Data.CopyTo(newData);
            return new QueueMessage
            {
                Id = Guid.NewGuid(),
                Data = newData.AsMemory(0, Data.Length),
                Route = Route,
                QueueName = queueName,
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