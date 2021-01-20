using System;

namespace MessageBroker.Client.Models
{
    public class QueueConsumerMessage
    {
        public string Route { get; set; }
        public Memory<byte> Data { get; set; }
    }
}