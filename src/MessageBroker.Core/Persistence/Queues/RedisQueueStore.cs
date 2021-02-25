using System;
using System.Collections.Generic;
using System.Linq;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Persistence.Redis;
using MessageBroker.Core.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Core.Persistence.Queues
{
    public class RedisQueueStore : IQueueStore
    {
        private const string QueueNameKey = "MessageBroker.Queue.Set";
        private readonly IRedisConnectionProvider _redisConnectionProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IQueue> _queues;

        public RedisQueueStore(IRedisConnectionProvider redisConnectionProvider, IServiceProvider serviceProvider)
        {
            _redisConnectionProvider = redisConnectionProvider;
            _serviceProvider = serviceProvider;
            _queues = new List<IQueue>();
        }

        public void Setup()
        {
            Logger.LogInformation("QueueStore: setting up");

            var connection = _redisConnectionProvider.Get();
            var results = connection.GetDatabase().SetScan(QueueNameKey);

            foreach (var result in results)
                try
                {
                    var queue = DeserializeIQueue(result);
                    _queues.Add(queue);
                }
                catch
                {
                    Logger.LogError("Failed to deserialize queue");
                }

            Logger.LogInformation($"QueueStore: found {results.Count()} queues");
        }

        public IEnumerable<IQueue> GetAll()
        {
            return _queues;
        }

        public void Add(string name, string route)
        {
            var queue = SetupQueue(name, route);
            _queues.Add(queue);
            var connection = _redisConnectionProvider.Get();
            var serializedQueue = SerializeIQueue(queue);
            connection.GetDatabase().SetAdd(QueueNameKey, serializedQueue);
        }

        public bool TryGetValue(string name, out IQueue queue)
        {
            queue = _queues.FirstOrDefault(q => q.Name == name);
            return queue != null;
        }

        public void Delete(string name)
        {
            var queueToRemove = _queues.FirstOrDefault(q => q.Name == name);

            if (queueToRemove == null)
                return;

            _queues.Remove(queueToRemove);
            var connection = _redisConnectionProvider.Get();
            var serializedQueue = SerializeIQueue(queueToRemove);
            connection.GetDatabase().SetRemove(QueueNameKey, serializedQueue);
        }

        private string SerializeIQueue(IQueue queue)
        {
            return string.Join(",", new {queue.Name, queue.Route});
        }

        private IQueue DeserializeIQueue(string value)
        {
            var valueParts = value.Split(",");

            var name = valueParts[0];
            var route = valueParts[1];

            return SetupQueue(name, route);
        }

        private IQueue SetupQueue(string name, string route)
        {
            var queue = _serviceProvider.GetService<IQueue>();
            queue.Setup(name, route);
            return queue;
        }
    }
}