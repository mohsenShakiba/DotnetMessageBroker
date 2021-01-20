namespace MessageBroker.Client.EventStores
{
    public enum SendEventType
    {
        Failed = 1,
        Sent = 2,
        Nack = 3,
        Ack = 4
    }
}