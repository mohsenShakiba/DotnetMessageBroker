using System;

namespace MessageBroker.Models
{
    public ref struct Error
    {
        public Guid Id { get; init; }
        public string Message { get; init; }
    }
}