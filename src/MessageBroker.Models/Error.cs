using System;

namespace MessageBroker.Models
{
    public struct Error
    {
        public Guid Id { get; init; }
        public string Message { get; init; }
    }
}