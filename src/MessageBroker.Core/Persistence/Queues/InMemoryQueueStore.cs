using System;
using System.Collections.Generic;
using System.Linq;
using MessageBroker.Core.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Core.Persistence.Queues
{
    public class InMemoryQueueStore : IQueueStore
    {
        private readonly List<IQueue> _queues;
        private readonly IServiceProvider _serviceProvider;

        public InMemoryQueueStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _queues = new List<IQueue>();
        }

        public void Setup()
        {
        }

        public IEnumerable<IQueue> GetAll()
        {
            return _queues;
        }

        public void Add(string name, string route)
        {
            var queue = SetupQueue(name, route);
            _queues.Add(queue);
        }

        public bool TryGetValue(string name, out IQueue queue)
        {
            queue = _queues.FirstOrDefault(q => q.Name == name);
            return queue != null;
        }

        public void Remove(string name)
        {
            var queueToRemove = _queues.FirstOrDefault(q => q.Name == name);

            if (queueToRemove == null)
                return;

            _queues.Remove(queueToRemove);
        }

        private IQueue SetupQueue(string name, string route)
        {
            var queue = _serviceProvider.GetService<IQueue>();
            queue.Setup(name, route);
            return queue;
        }
    }
}