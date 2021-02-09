using System;

namespace MessageBroker.Client.Models
{
    public class QueueConsumerMessage
    {
        public Guid MessageId { get; set; }
        public string Route { get; set; }
        public string QueueName { get; set; }
        public Memory<byte> Data { get; set; }
    }
}