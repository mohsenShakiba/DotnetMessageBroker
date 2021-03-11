using System;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.Subscription
{
    public interface ISubscriber: IAsyncDisposable
    {
        string Name { get; }
        string Route { get; }
        event Action<QueueConsumerMessage> MessageReceived;
    }
}