using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence.Messages.InMemoryStore
{
    public class InMemoryMessageStore : IMessageStore
    {
        private readonly ConcurrentDictionary<Guid, InMemoryMessage> _store;


        public InMemoryMessageStore()
        {
            _store = new ConcurrentDictionary<Guid, InMemoryMessage>();
        }


        public void Setup()
        {
            // do nothing
        }

        public void Add(QueueMessage message)
        {
            var inMemoryMessage = ObjectPool.Shared.Rent<InMemoryMessage>();

            inMemoryMessage.FillFrom(message);

            _store[message.Id] = inMemoryMessage;
        }

        public bool TryGetValue(Guid id, out QueueMessage message)
        {
            if (_store.TryGetValue(id, out var inMemoryMessage))
            {
                var buff = ArrayPool<byte>.Shared.Rent(inMemoryMessage.Data.Length);

                inMemoryMessage.Data.CopyTo(buff);

                message = new QueueMessage
                {
                    Id = id,
                    Route = inMemoryMessage.Route,
                    QueueName = inMemoryMessage.QueueName,
                    Data = buff.AsMemory(0, inMemoryMessage.Data.Length),
                    OriginalMessageData = buff
                };

                return true;
            }

            message = default;

            return false;
        }

        public void Delete(Guid id)
        {
            if (_store.TryRemove(id, out var persistedMessage)) ObjectPool.Shared.Return(persistedMessage);
        }

        public IEnumerable<Guid> PendingMessages(int count)
        {
            return _store.Keys;
        }
    }
}