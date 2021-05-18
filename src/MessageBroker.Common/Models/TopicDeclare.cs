using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Model for declaring a queue
    /// Ignore if the topic already exists
    /// </summary>
    public struct TopicDeclare
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
    }
}