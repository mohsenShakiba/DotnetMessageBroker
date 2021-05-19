using System;
using System.Collections.Concurrent;

namespace MessageBroker.Core.Clients.Store
{
    /// <inheritdoc />
    public class ClientStore : IClientStore
    {
        private readonly ConcurrentDictionary<Guid, IClient> _sendQueues;

        /// <summary>
        /// Instantiates a new <see cref="ClientStore" />
        /// </summary>
        public ClientStore()
        {
            _sendQueues = new ConcurrentDictionary<Guid, IClient>();
        }


        /// <inheritdoc />
        public void Add(IClient client)
        {
            _sendQueues[client.Id] = client;
        }

        /// <inheritdoc />
        public void Remove(IClient client)
        {
            _sendQueues.TryRemove(client.Id, out var _);
        }

        /// <inheritdoc />
        public bool TryGet(Guid clientId, out IClient client)
        {
            return _sendQueues.TryGetValue(clientId, out client);
        }
    }
}