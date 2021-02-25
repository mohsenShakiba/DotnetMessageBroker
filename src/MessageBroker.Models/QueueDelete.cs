using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     will delete the queue if exists
    /// </summary>
    public struct QueueDelete
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }
}