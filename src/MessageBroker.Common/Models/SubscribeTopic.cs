using System;

namespace MessageBroker.Models
{
    /// <summary>
    /// Model for subscribing to topic if exists
    /// </summary>
    public struct SubscribeTopic
    {
        public Guid Id { get; init; }
        public string TopicName { get; init; }
    }
}