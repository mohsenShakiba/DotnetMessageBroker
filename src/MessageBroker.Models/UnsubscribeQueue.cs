using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     will unsubscribe the queue if possible
    /// </summary>
    public struct UnsubscribeQueue
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
    }
}