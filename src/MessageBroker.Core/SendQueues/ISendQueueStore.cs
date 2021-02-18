using System;
using MessageBroker.Socket.Client;

namespace MessageBroker.Core
{
    public interface ISendQueueStore
    {
        void Add(IClientSession clientSession);
        void Remove(IClientSession clientSession);
        bool TryGet(Guid sessionId, out ISendQueue sendQueue);
    }
}