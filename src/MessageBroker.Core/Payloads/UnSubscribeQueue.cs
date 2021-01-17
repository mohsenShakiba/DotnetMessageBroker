using System;

namespace MessageBroker.Core.Models
{
    /// <summary>
    /// will unsubscribe the queue if possible
    /// </summary>
    public ref struct UnSubscribeQueue
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
    }
}