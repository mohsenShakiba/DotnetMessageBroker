using System;

namespace MessageBroker.Common.Models
{
    /// <summary>
    /// Error containing message for when something goes wrong
    /// </summary>
    public struct Error
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
    }
}