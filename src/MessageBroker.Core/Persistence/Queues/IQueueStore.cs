using System.Collections.Generic;
using MessageBroker.Core.Queues;

namespace MessageBroker.Core.Persistence.Queues
{
    public interface IQueueStore
    {
        void Setup();
        IEnumerable<IQueue> GetAll();
        void Add(string name, string route);
        bool TryGetValue(string name, out IQueue queue);
        void Delete(string name);
    }
}