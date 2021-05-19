using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Configuration related to client
    /// </summary>
    public struct ConfigureClient
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Max number of messages can be sent to client before receiving any ack or nack
        /// </summary>
        public int PrefetchCount { get; set; }
    }
}