using System;
using System.Collections.Concurrent;

namespace MessageBroker.Core.Clients.Store
{
    public class ClientStore : IClientStore
    {
        private readonly ConcurrentDictionary<Guid, IClient> _sendQueues;

        public ClientStore()
        {
            _sendQueues = new ConcurrentDictionary<Guid, IClient>();
        }


        public void Add(IClient client)
        {
            _sendQueues[client.Id] = client;
        }

        public void Remove(IClient client)
        {
            _sendQueues.TryRemove(client.Id, out var _);
        }

        public bool TryGet(Guid clientId, out IClient queue)
        {
            return _sendQueues.TryGetValue(clientId, out queue);
        }
    }
}