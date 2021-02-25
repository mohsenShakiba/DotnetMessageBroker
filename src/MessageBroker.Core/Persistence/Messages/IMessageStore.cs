using System;
using System.Collections.Generic;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence.Messages
{
    public interface IMessageStore
    {
        void Setup();
        void Add(QueueMessage message);
        bool TryGetValue(Guid id, out QueueMessage message);
        void Delete(Guid id);
        IEnumerable<Guid> PendingMessages(int count);
    }
}