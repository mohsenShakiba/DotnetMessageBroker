using MessageBroker.Client.Subscription;
using MessageBroker.Models;

namespace MessageBroker.Client.QueueConsumerCoordination
{
    public interface ISubscriberStore
    {
        void Add(Subscriber subscriber);
        void Remove(Subscriber subscriber);
        void OnMessage(QueueMessage queueMessage);
    }
}