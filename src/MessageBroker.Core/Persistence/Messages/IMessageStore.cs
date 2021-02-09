using System;
using System.Collections.Generic;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence
{
    public interface IMessageStore
    {
        void Setup();
        void InsertAsync(QueueMessage message);
        bool TryGetValue(Guid id, out QueueMessage message);
        void DeleteAsync(Guid id);
        IEnumerable<Guid> PendingMessages(int count);
    }
}