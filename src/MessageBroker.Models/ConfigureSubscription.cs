using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     sent by subscribers to provide basic configuration
    /// </summary>
    public ref struct ConfigureSubscription
    {
        public Guid Id { get; init; }
        public int Concurrency { get; init; }
        public bool AutoAck { get; init; }
    }
}