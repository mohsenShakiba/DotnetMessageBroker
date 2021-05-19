using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Model for deleting a queue
    /// </summary>
    public struct TopicDelete
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}