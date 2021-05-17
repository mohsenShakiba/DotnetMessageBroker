using System;

namespace MessageBroker.Models
{
    /// <summary>
    /// Indicates the payload process failed by the client
    /// </summary>
    public struct Nack
    {
        public Guid Id { get; init; }
    }
}