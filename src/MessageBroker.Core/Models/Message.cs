using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    public struct Message
    {
        public Guid Id { get; init; }
        public string Route { get; init; }
        public Memory<byte> Data { get; init; }
        public IMemoryOwner<byte> OriginalMessageMemoryOwner { get; init; }
    }
}
