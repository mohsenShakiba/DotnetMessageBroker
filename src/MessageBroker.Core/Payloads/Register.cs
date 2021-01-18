using System;

namespace MessageBroker.Core.Payloads
{
    /// <summary>
    /// sent by subscribers to provide basic configuration
    /// </summary>
    public ref struct Register
    {
        public Guid Id { get; init; }
        public int Concurrency { get; init; }
    }
    
}
