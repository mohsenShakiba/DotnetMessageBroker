using System;
using MessageBroker.Models;

namespace MessageBroker.Serialization
{
    public interface ISerializer
    {
        SendPayload ToSendPayload(Ack ack);
        SendPayload ToSendPayload(Nack nack);
        SendPayload ToSendPayload(Message msg);
        SendPayload ToSendPayload(SubscribeQueue subscribeQueue);
        SendPayload ToSendPayload(UnsubscribeQueue subscribeQueue);
        SendPayload ToSendPayload(QueueDeclare queueDeclare);
        SendPayload ToSendPayload(QueueDelete queueDelete);
        SendPayload ToSendPayload(ConfigureSubscription configureSubscription);

        PayloadType ParsePayloadType(Memory<byte> b);

        Ack ToAck(Memory<byte> data);
        Message ToMessage(Memory<byte> data);
        SubscribeQueue ToSubscribeQueue(Memory<byte> data);
        UnsubscribeQueue ToUnsubscribeQueue(Memory<byte> data);
        QueueDeclare ToQueueDeclareModel(Memory<byte> data);
        QueueDelete ToQueueDeleteModel(Memory<byte> data);
        ConfigureSubscription ToConfigureSubscription(Memory<byte> data);
    }
}