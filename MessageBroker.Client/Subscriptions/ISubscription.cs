using System;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.Subscriptions
{
    /// <summary>
    /// Object used by client to subscribe to messages of a topic
    /// </summary>
    public interface ISubscription: IAsyncDisposable
    {
        /// <summary>
        /// Name of topic
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Route of topic
        /// </summary>
        string Route { get; }
        
        /// <summary>
        /// Called when new message is received from topic
        /// </summary>
        event Action<SubscriptionMessage> MessageReceived;
    }
}