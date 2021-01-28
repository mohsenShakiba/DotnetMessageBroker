using System.Collections.Generic;
using MessageBroker.Models;

namespace MessageBroker.Core.Queues
{
    public interface IQueueStore
    {
        void Add(QueueDeclare queueDeclare);
        void Remove(string name);
        bool Exists(string name);
        
        IQueue Get(string name);
        bool TryGetValue(string name, out IQueue queue);
        IEnumerable<IQueue> Match(string route);
        void Dispatch(string messageRoute, Message message);
        IEnumerable<IQueue> Queues { get; }
    }
}