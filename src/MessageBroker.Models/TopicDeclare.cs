using System;

namespace MessageBroker.Models
{
    /// <summary>
    /// Model for declaring a queue
    /// Ignore if the topic already exists
    /// </summary>
    public struct TopicDeclare
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Route { get; init; }
    }
}