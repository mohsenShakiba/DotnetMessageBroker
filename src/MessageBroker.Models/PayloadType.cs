namespace MessageBroker.Models
{
    /// <summary>
    /// Types of payloads
    /// </summary>
    public enum PayloadType
    {
        Msg = 1,
        Ok = 2,
        Error = 3,
        Ack = 4,
        Nack = 5,
        TopicDeclare = 6,
        TopicDelete = 7,
        SubscribeTopic = 8,
        UnsubscribeTopic = 9,
        Ready = 10,
        Configure = 11,
        TopicMessage = 12
    }
}