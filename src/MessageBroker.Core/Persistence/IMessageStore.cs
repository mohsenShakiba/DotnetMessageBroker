using System;
using System.Collections.Generic;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence
{
    public interface IMessageStore
    {
        void SetupAsync();
        void InsertAsync(Message message);
        bool TryGetValue(Guid id, out Message message);
        void DeleteAsync(Guid id);
        IEnumerable<Guid> PendingMessages(int count);
    }
}