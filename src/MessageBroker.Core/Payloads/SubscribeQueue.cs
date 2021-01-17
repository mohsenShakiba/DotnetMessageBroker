using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    /// <summary>
    /// will subscribe the queue if exists
    /// </summary>
    public ref struct SubscribeQueue
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
    };
}
