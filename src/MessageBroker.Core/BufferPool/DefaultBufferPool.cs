using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public class DefaultBufferPool : IBufferPool
    {


        public DefaultBufferPool()
        {
        }

        public byte[] Rent(int size)
        {
            var memoryOwner = ArrayPool<byte>.Shared.Rent(size);
            return memoryOwner;
        }

        public void Return(byte[] data)
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }
}
