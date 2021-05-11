using System;
using System.Collections.Generic;
using System.Linq;
using MessageBroker.Core.Topics;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Core.Persistence.Topics
{
    /// <inheritdoc />
    public class InMemoryTopicStore : ITopicStore
    {
        private readonly List<ITopic> _queues;
        private readonly IServiceProvider _serviceProvider;

        public InMemoryTopicStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _queues = new List<ITopic>();
        }

        public void Setup()
        {
            // no-op
        }

        public IEnumerable<ITopic> GetAll()
        {
            return _queues;
        }

        public void Add(string name, string route)
        {
            var queue = SetupQueue(name, route);
            _queues.Add(queue);
        }

        public bool TryGetValue(string name, out ITopic topic)
        {
            topic = _queues.FirstOrDefault(q => q.Name == name);
            return topic != null;
        }

        public void Delete(string name)
        {
            var queueToRemove = _queues.FirstOrDefault(q => q.Name == name);

            if (queueToRemove == null)
                return;

            _queues.Remove(queueToRemove);
        }

        private ITopic SetupQueue(string name, string route)
        {
            var queue = _serviceProvider.GetRequiredService<ITopic>();
            queue.Setup(name, route);
            return queue;
        }
    }
}