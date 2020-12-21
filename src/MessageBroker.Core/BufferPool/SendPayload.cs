using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public ref struct SendPayload
    {
        public IMemoryOwner<byte> MemoryOwner { get; init; }
        public Memory<byte> Data { get; init; }

        public void Dispose()
        {
            MemoryOwner.Dispose();
        }

    }
}
