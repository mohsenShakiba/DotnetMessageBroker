namespace MessageBroker.Models
{
    /// <summary>
    ///     types of payload
    /// </summary>
    public enum PayloadType
    {
        Msg = 1,
        Ok = 2,
        Error = 3,
        Ack = 4,
        Nack = 5,
        ConfigureSubscription = 6,
        QueueCreate = 7,
        QueueDelete = 8,
        SubscribeQueue = 9,
        UnSubscribeQueue = 10,
        Ready = 11
    }
}