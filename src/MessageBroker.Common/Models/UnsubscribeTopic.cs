using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Model for unsubscribing topic if subscribed
    /// </summary>
    public struct UnsubscribeTopic
    {
        public Guid Id { get; set; }
        public string TopicName { get; set; }
    }
}