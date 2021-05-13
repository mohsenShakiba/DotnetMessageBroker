using System;
using MessageBroker.Core.Stats.TopicStatus;
using MessageBroker.Core.Topics;

namespace MessageBroker.Core.Broker
{
    /// <summary>
    /// Abstraction for Broker
    /// </summary>
    /// <seealso cref="Broker"/>
    public interface IBroker: IDisposable
    {
        /// <summary>
        /// Start the broker
        /// </summary>
        public void Start();
        
        /// <summary>
        /// Stop and dispose the broker
        /// </summary>
        public void Stop();

        public ITopic GetTopic(string name);
    }
}