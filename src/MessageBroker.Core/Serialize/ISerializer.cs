using MessageBroker.Core.BufferPool;
using MessageBroker.Core.Models;
using MessageBroker.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Serialize
{
    public interface ISerializer
    {
        SendPayload ToSendPayload(Ack ack);

        SendPayload ToSendPayload(Message msg);
        SendPayload ToSendPayload(Subscribe subscribe);
        SendPayload ToSendPayload(Listen listen);
        SendPayload ToSendPayload(QueueDeclare queue);
        SendPayload ToSendPayload(QueueDelete queue);

        PayloadType ParsePayloadType(Memory<byte> b);

        Ack ToAck(Span<byte> data);

        Message ToMessage(Span<byte> data);

        Listen ToListenRoute(Span<byte> data);

        Subscribe ToSubscribe(Span<byte> data);
        QueueDeclare ToQueueDeclareModel(Span<byte> data);
        QueueDelete ToQueueDeleteModel(Span<byte> data);
    }
}
