using System;

namespace MessageBroker.Models.Models
{
    /// <summary>
    ///     will subscribe the queue if exists
    /// </summary>
    public ref struct SubscribeQueue
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
    }
}