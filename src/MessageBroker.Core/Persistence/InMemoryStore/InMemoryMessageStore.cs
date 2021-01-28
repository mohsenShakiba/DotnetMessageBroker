﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence.InMemoryStore
{
    public class InMemoryMessageStore : IMessageStore
    {
        private readonly ConcurrentDictionary<Guid, InMemoryMessage> _store;
        

        public InMemoryMessageStore()
        {
            _store = new ();
        }


        public void SetupAsync()
        {
            // do nothing
        }

        public void InsertAsync(Message message)
        {
            var inMemoryMessage = ObjectPool.Shared.Rent<InMemoryMessage>();
            
            inMemoryMessage.FillFrom(message);
            
            _store[message.Id] = inMemoryMessage;
        }

        public bool TryGetValue(Guid id, out Message message)
        {
            if (_store.TryGetValue(id, out var inMemoryMessage))
            {
                var buff = ArrayPool<byte>.Shared.Rent(inMemoryMessage.Data.Length);

                message = new Message
                {
                    Id = id,
                    Route = inMemoryMessage.Route,
                    Data = buff.AsMemory(0, inMemoryMessage.Data.Length),
                    OriginalMessageData = buff
                };

                return true;
            }

            message = new Message();

            return false;
        }

        public void DeleteAsync(Guid id)
        {
            if (_store.TryRemove(id, out var persistedMessage))
            {
                ObjectPool.Shared.Return(persistedMessage);
            }
        }

        public IEnumerable<Guid> PendingMessages(int count)
        {
            return new Guid[0];
        }

    }
}