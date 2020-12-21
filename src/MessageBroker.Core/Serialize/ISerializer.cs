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

        PayloadType ParsePayloadType(Memory<byte> b);

        Ack ToAck(Span<byte> data);

        Message ToMessage(Span<byte> data);

        Listen ToListenRoute(Span<byte> data);

        Subscribe ToSubscribe(Span<byte> data);
    }
}
