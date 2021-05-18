using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Indicates the payload process failed by the client
    /// </summary>
    public struct Nack
    {
        public Guid Id { get; set; }
    }
}