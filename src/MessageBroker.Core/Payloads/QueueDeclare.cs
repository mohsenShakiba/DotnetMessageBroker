using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    /// <summary>
    /// will create a new queue, if not exists
    /// </summary>
    public ref struct QueueDeclare
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public string Route { get; init; }
    }
}
