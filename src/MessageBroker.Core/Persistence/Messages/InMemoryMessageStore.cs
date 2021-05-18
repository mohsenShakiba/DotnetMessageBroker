using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MessageBroker.Common.Models;

namespace MessageBroker.Core.Persistence.Messages
{
    /// <inheritdoc />
    public class InMemoryMessageStore : IMessageStore
    {
        private readonly ConcurrentDictionary<Guid, TopicMessage> _store;


        public InMemoryMessageStore()
        {
            _store = new ConcurrentDictionary<Guid, TopicMessage>();
        }


        public void Setup()
        {
            // do nothing
        }

        public void Add(TopicMessage message)
        {
            _store[message.Id] = message;
        }

        public bool TryGetValue(Guid id, out TopicMessage message)
        {
            if (_store.TryGetValue(id, out var topicMessage))
            {
                message = topicMessage;

                return true;
            }

            message = default;

            return false;
        }

        public void Delete(Guid id)
        {
            if (_store.TryRemove(id, out var topicMessage)) topicMessage.Dispose();
            ;
        }

        public IEnumerable<Guid> GetAll()
        {
            return _store.Keys;
        }
    }
}