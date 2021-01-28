using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     indicating the payload process failed
    /// </summary>
    public ref struct Nack
    {
        public Guid Id { get; init; }
    }
}