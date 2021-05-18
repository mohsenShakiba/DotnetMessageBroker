using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Model for subscribing to topic if exists
    /// </summary>
    public struct SubscribeTopic
    {
        public Guid Id { get; set; }
        public string TopicName { get; set; }
    }
}