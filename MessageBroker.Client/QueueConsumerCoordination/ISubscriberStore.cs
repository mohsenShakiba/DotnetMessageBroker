using System;
using MessageBroker.Client.Subscription;
using MessageBroker.Models;

namespace MessageBroker.Client.QueueConsumerCoordination
{
    public interface ISubscriberStore: IAsyncDisposable
    {
        void Add(Subscriber subscriber);
        void Remove(Subscriber subscriber);
        void OnMessage(QueueMessage queueMessage);
    }
}