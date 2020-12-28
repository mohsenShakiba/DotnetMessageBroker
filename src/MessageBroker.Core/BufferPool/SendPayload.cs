using MessageBroker.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public class SendPayload
    {
        public IMemoryOwner<byte> MemoryOwner { get; set; }
        public Memory<byte> Data { get; set; }
        public Guid Id { get; set; }
        public Span<byte> DataWithoutSize => Data.Slice(4).Span;

        public void Dispose()
        {
            MemoryOwner.Dispose();
        }

    }

}
