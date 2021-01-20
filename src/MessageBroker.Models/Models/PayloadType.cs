namespace MessageBroker.Models.Models
{
    /// <summary>
    ///     types of payload
    /// </summary>
    public enum PayloadType
    {
        Msg = 1,
        Ack = 2,
        Nack = 3,
        SubscribeQueue = 4,
        UnSubscribeQueue = 5,
        Register = 6,
        QueueCreate = 7,
        QueueDelete = 8
    }
}