using System;

namespace MessageBroker.Models
{
    /// <summary>
    /// Indicates the payload was received and processed by the server
    /// </summary>
    public struct Ok
    {
        public Guid Id { get; init; }
    }
}