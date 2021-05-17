using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MessageBroker.Client.Subscriptions.Store
{
    /// <inheritdoc />
    public class SubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, ISubscription> _queueDict;

        public SubscriptionStore()
        {
            _queueDict = new ConcurrentDictionary<string, ISubscription>();
        }

        public void Add(string name, ISubscription subscription)
        {
            // we are using name because Subscription.Name is still null at this point
            _queueDict[name] = subscription;
        }

        public void Remove(ISubscription subscription)
        {
            _queueDict.TryRemove(subscription.Name, out _);
        }

        public bool TryGet(string topicName, out ISubscription subscription)
        {
            return _queueDict.TryGetValue(topicName, out subscription);
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