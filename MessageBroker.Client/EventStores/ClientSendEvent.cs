using System;

namespace MessageBroker.Client.EventStores
{
    public class ClientSendEvent
    {
        public Guid Id { get; init; }
        public SendEventType EventType { get; set; }
    }
}