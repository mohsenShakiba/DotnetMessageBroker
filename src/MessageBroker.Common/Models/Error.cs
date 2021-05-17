using System;

namespace MessageBroker.Models
{
    /// <summary>
    /// Error containing message for when something goes wrong
    /// </summary>
    public struct Error
    {
        public Guid Id { get; init; }
        public string Message { get; init; }
    }
}