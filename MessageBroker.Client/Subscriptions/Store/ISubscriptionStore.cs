using System;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Models;

namespace MessageBroker.Client.QueueConsumerCoordination
{
    /// <summary>
    /// Store for <see cref="ISubscription"/>
    /// </summary>
    public interface ISubscriptionStore: IAsyncDisposable
    {
        /// <summary>
        /// Called when a new subscription is created
        /// </summary>
        /// <param name="topicName">Name of topic</param>
        /// <param name="subscription">subscription object</param>
        void Add(string topicName, ISubscription subscription);
        
        /// <summary>
        /// Called when a subscription is removed
        /// </summary>
        /// <param name="subscription">subscription object</param>
        void Remove(ISubscription subscription);
        
        /// <summary>
        /// Try to get a <see cref="ISubscription"/> by topic name if exists
        /// </summary>
        /// <param name="topicName">Name of topic</param>
        /// <param name="subscription">Subscription if found</param>
        /// <returns>True if subscription is found by topic name</returns>
        bool TryGet(string topicName, out ISubscription subscription);
    }
}