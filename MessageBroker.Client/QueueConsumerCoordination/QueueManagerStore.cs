using System.Collections.Concurrent;
using MessageBroker.Client.QueueManagement;
using MessageBroker.Models;

namespace MessageBroker.Client.QueueConsumerCoordination
{
    public class QueueManagerStore : IQueueManagerStore
    {
        private readonly ConcurrentDictionary<string, QueueManager> _queueDict;

        public QueueManagerStore()
        {
            _queueDict = new ConcurrentDictionary<string, QueueManager>();
        }

        public void Add(QueueManager queueManager)
        {
            _queueDict[queueManager.Name] = queueManager;
        }

        public void Remove(QueueManager queueManager)
        {
            _queueDict.TryRemove(queueManager.Name, out _);
        }

        public void OnMessage(QueueMessage queueMessage)
        {
            if (_queueDict.TryGetValue(queueMessage.QueueName, out var queueConsumer))
                queueConsumer.OnMessage(queueMessage);
        }
    }
}