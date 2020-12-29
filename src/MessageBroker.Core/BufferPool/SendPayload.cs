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
        public byte[] OriginalData { get; set; }
        public int PayloadSize { get; set; }
        public Guid Id { get; set; }
        public Memory<byte> DataWithoutSize => OriginalData.AsMemory(4, PayloadSize - 4);
        public Memory<byte> Data => OriginalData.AsMemory(0, PayloadSize);
    }

}
