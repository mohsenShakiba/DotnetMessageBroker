using System;

namespace MessageBroker.Core.InternalEventChannel
{
    public class InternalEvent
    {
        public Guid SessionId { get; set; }
        public Guid MessageId { get; set; }
        public bool Ack { get; set; }
        public bool AutoAck { get; set; }
        
    }
}