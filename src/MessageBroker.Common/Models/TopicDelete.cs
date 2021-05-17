using System;

namespace MessageBroker.Models
{
    /// <summary>
    /// Model for deleting a queue
    /// </summary>
    public struct TopicDelete
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }
}