using System;
using MessageBroker.Models;

namespace MessageBroker.Serialization
{
    /// <summary>
    /// Deserializes a binary payload to structured Payloads
    /// </summary>
    public interface IDeserializer
    {
        PayloadType ParsePayloadType(Memory<byte> b);

        Message ToMessage(Memory<byte> data);
        TopicMessage ToTopicMessage(Memory<byte> data);
        Ack ToAck(Memory<byte> data);
        Nack ToNack(Memory<byte> data);
        Error ToError(Memory<byte> data);
        Ok ToOk(Memory<byte> data);
        SubscribeTopic ToSubscribeTopic(Memory<byte> data);
        UnsubscribeTopic ToUnsubscribeTopic(Memory<byte> data);
        TopicDeclare ToTopicDeclare(Memory<byte> data);
        TopicDelete ToTopicDelete(Memory<byte> data);
        ConfigureClient ToConfigureClient(Memory<byte> data);
    }
}