using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MessageBroker.Client.Subscription;
using MessageBroker.Models;

namespace MessageBroker.Client.QueueConsumerCoordination
{
    public class SubscriberStore : ISubscriberStore
    {
        private readonly ConcurrentDictionary<string, Subscriber> _queueDict;

        public SubscriberStore()
        {
            _queueDict = new ConcurrentDictionary<string, Subscriber>();
        }

        public void Add(Subscriber subscriber)
        {
            _queueDict[subscriber.Name] = subscriber;
        }

        public void Remove(Subscriber subscriber)
        {
            _queueDict.TryRemove(subscriber.Name, out _);
        }

        public void OnMessage(QueueMessage queueMessage)
        {
            if (_queueDict.TryGetValue(queueMessage.QueueName, out var queueConsumer))
                queueConsumer.OnMessage(queueMessage);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var (_, subscriber) in _queueDict)
            {
                await subscriber.DisposeAsync();
            }
        }
    }
}