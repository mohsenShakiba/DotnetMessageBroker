using System;

namespace MessageBroker.Models
{
    /// <summary>
    /// Model for unsubscribing topic if subscribed
    /// </summary>
    public struct UnsubscribeTopic
    {
        public Guid Id { get; init; }
        public string TopicName { get; init; }
    }
}