using System;
using MessageBroker.Models;

namespace MessageBroker.Serialization
{
    public interface ISerializer
    {
        SerializedPayload Serialize(Ack ack);
        SerializedPayload Serialize(Nack nack);
        SerializedPayload Serialize(Message msg);
        SerializedPayload Serialize(QueueMessage msg);
        SerializedPayload Serialize(SubscribeQueue subscribeQueue);
        SerializedPayload Serialize(UnsubscribeQueue subscribeQueue);
        SerializedPayload Serialize(QueueDeclare queueDeclare);
        SerializedPayload Serialize(QueueDelete queueDelete);
        SerializedPayload Serialize(ConfigureSubscription configureSubscription);
        SerializedPayload Serialize(Error error);

        PayloadType ParsePayloadType(Memory<byte> b);

        Ack ToAck(Memory<byte> data);
        Nack ToNack(Memory<byte> data);
        Message ToMessage(Memory<byte> data);
        QueueMessage ToQueueMessage(Memory<byte> data);
        SubscribeQueue ToSubscribeQueue(Memory<byte> data);
        UnsubscribeQueue ToUnsubscribeQueue(Memory<byte> data);
        QueueDeclare ToQueueDeclareModel(Memory<byte> data);
        QueueDelete ToQueueDeleteModel(Memory<byte> data);
        ConfigureSubscription ToConfigureSubscription(Memory<byte> data);
        Error ToError(Memory<byte> data);
    }
}