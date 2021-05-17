using System;
using System.Collections.Generic;
using System.Linq;
using MessageBroker.Core.Persistence.Redis;
using MessageBroker.Core.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Core.Persistence.Topics
{
    /// <inheritdoc />
    public class RedisTopicStore : ITopicStore
    {
        private const string QueueNameKey = "MessageBroker.Queue.Set";
        private readonly RedisConnectionProvider _redisConnectionProvider;
        private readonly ILogger<RedisTopicStore> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<ITopic> _queues;

        public RedisTopicStore(RedisConnectionProvider redisConnectionProvider, ILogger<RedisTopicStore> logger, IServiceProvider serviceProvider)
        {
            _redisConnectionProvider = redisConnectionProvider;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _queues = new List<ITopic>();
        }

        public void Setup()
        {
            var connection = _redisConnectionProvider.Get();
            var results = connection.GetDatabase().SetScan(QueueNameKey);

            foreach (var result in results)
            {
                try
                {
                    var queue = DeserializeIQueue(result);
                    _queues.Add(queue);
                }
                catch
                {
                    _logger.LogError("Failed to deserialize topic data");
                }
            }

            _logger.LogInformation($"Found {results.Count()} topics");
        }

        public IEnumerable<ITopic> GetAll()
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
            var connection = _redisConnectionProvider.Get();
            var serializedQueue = SerializeIQueue(queueToRemove);
            connection.GetDatabase().SetRemove(QueueNameKey, serializedQueue);
        }

        private string SerializeIQueue(ITopic topic)
        {
            return string.Join(",", new {topic.Name, topic.Route});
        }

        private ITopic DeserializeIQueue(string value)
        {
            var valueParts = value.Split(",");

            var name = valueParts[0];
            var route = valueParts[1];

            return SetupQueue(name, route);
        }

        private ITopic SetupQueue(string name, string route)
        {
            var queue = _serviceProvider.GetService<ITopic>();
            queue.Setup(name, route);
            queue.StartProcessingMessages();
            return queue;
        }
    }
}