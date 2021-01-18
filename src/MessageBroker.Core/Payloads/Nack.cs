using System;

namespace MessageBroker.Core.Payloads
{
    /// <summary>
    /// indicating the payload process failed
    /// </summary>
    public ref struct Nack
    {
        public Guid Id { get; init; }
    }
}