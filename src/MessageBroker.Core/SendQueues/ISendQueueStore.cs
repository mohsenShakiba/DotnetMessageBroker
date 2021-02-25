using System;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    public interface ISendQueueStore
    {
        void Add(IClientSession clientSession, ISendQueue sendQueue = null);
        void Remove(IClientSession clientSession);
        bool TryGet(Guid sessionId, out ISendQueue sendQueue);
    }
}