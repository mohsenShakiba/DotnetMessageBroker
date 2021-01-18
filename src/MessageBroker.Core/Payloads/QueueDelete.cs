using System;

namespace MessageBroker.Core.Payloads
{
    /// <summary>
    /// will delete the queue if exists
    /// </summary>
    public ref struct QueueDelete
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }
}
