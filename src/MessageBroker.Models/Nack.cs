using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     indicating the payload process failed
    /// </summary>
    public struct Nack
    {
        public Guid Id { get; init; }
    }
}