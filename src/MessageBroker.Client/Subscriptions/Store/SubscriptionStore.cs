using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MessageBroker.Client.Subscriptions.Store
{
    /// <inheritdoc />
    public class SubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, ISubscription> _queueDict;

        /// <summary>
        /// Instantiates a new <see cref="SubscriptionStore" />
        /// </summary>
        public SubscriptionStore()
        {
            _queueDict = new ConcurrentDictionary<string, ISubscription>();
        }

        /// <inheritdoc />
        public void Add(string name, ISubscription subscription)
        {
            // we are using name because Subscription.Name is still null at this point
            _queueDict[name] = subscription;
        }

        /// <inheritdoc />
        public void Remove(ISubscription subscription)
        {
            _queueDict.TryRemove(subscription.Name, out _);
        }

        /// <inheritdoc />
        public bool TryGet(string topicName, out ISubscription subscription)
        {
            return _queueDict.TryGetValue(topicName, out subscription);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            foreach (var (_, subscriber) in _queueDict) await subscriber.DisposeAsync();
        }
    }
}