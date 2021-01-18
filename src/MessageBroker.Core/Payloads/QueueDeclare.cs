using System;

namespace MessageBroker.Core.Payloads
{
    /// <summary>
    /// will create a new queue, if not exists
    /// </summary>
    public ref struct QueueDeclare
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Route { get; init; }
    }
}
