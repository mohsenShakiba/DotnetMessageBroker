using System;

namespace MessageBroker.Models.Models
{
    /// <summary>
    ///     will delete the queue if exists
    /// </summary>
    public ref struct QueueDelete
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }
}