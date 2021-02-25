using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     will subscribe the queue if exists
    /// </summary>
    public struct SubscribeQueue
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
        public int Concurrency { get; init; }
    }
}