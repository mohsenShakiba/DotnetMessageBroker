using System;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    public interface ISendQueueStore
    {
        ISendQueue Add(IClientSession clientSession, ISendQueue sendQueue = null);
        ISendQueue Remove(IClientSession clientSession);
        bool TryGet(Guid sessionId, out ISendQueue sendQueue);
    }
}