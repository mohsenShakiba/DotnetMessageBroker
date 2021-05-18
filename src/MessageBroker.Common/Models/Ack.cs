using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Indicating the payload process was successful by the client
    /// </summary>
    public struct Ack
    {
        public Guid Id { get; set; }
    }
}