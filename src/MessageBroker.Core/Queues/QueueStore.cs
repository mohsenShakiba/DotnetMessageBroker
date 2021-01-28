using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Core.Queues
{
    public class QueueStore : IQueueStore
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly Dictionary<string, IQueue> _queues;

        public QueueStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _queues = new();
        }


        public void Add(QueueDeclare queueDeclare)
        {
            // create queue
            var queue = _serviceProvider.GetRequiredService<IQueue>();

            // setup queue with name and route
            queue.Setup(queueDeclare.Name, queueDeclare.Route);

            // add queue to list of queues
            _queues[queueDeclare.Name] = queue;
        }

        public void Remove(string name)
        {
            _queues.Remove(name, out _);
        }

        public bool Exists(string name)
        {
            return _queues.TryGetValue(name, out _);
        }

        public IQueue Get(string name)
        {
            if (_queues.TryGetValue(name, out var queue))
                return queue;

            return null;
        }

        public bool TryGetValue(string name, out IQueue queue)
        {
            return _queues.TryGetValue(name, out queue);
        }

        public IEnumerable<IQueue> Match(string messageRoute)
        {
            foreach (var (_, queue) in _queues)
                if (queue.MessageRouteMatch(messageRoute))
                    yield return queue;
        }
        
        // todo: move to a better location
        public void Dispatch(string messageRoute, Message message)
        {
            foreach(var (_, queue) in _queues)
                if (queue.MessageRouteMatch(messageRoute))
                    queue.OnMessage(message);
        }

        public IEnumerable<IQueue> Queues => _queues.Values;
    }
}