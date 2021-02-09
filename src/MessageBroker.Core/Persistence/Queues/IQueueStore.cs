using System.Collections.Generic;
using System.Threading.Tasks;
using MessageBroker.Core.Queues;

namespace MessageBroker.Core.Persistence.Queues
{
    public interface IQueueStore
    {
        void Setup();
        IEnumerable<IQueue> GetAll();
        void Add(string name, string route);
        bool TryGetValue(string name, out IQueue queue);
        void Remove(string name);
    }
}