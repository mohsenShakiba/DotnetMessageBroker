using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    public ref struct Subscribe
    {
        public Guid Id { get; init; }
        public int Concurrency { get; init; }
    }
}
