using System;

namespace MessageBroker.Core.Clients.Store
{
    public interface IClientStore
    {
        void Add(IClient client);
        void Remove(IClient client);
        bool TryGet(Guid sessionId, out IClient queue);
    }
}