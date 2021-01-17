using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    /// <summary>
    /// will delete the queue if exists
    /// </summary>
    public ref struct QueueDelete
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
    }
}
