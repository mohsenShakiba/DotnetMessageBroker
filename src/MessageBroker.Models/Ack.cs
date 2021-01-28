using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     indicating the payload process was successful
    /// </summary>
    public ref struct Ack
    {
        public Guid Id { get; init; }
    }
}