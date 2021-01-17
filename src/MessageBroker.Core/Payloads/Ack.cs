using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    /// <summary>
    /// indicating the payload process was successful
    /// </summary>
    public ref struct Ack
    {
        public Guid Id { get; init; }
    }
}
