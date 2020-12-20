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

        private readonly ConcurrentDictionary<Guid, IMemoryOwner<byte>> _messageMemoryMap;

        public DefaultBufferPool()
        {
            _messageMemoryMap = new();
        }

        public void Release(Guid messageId)
        {
            if (_messageMemoryMap.TryGetValue(messageId, out var memoryOwner))
            {
                memoryOwner.Dispose();
            }
        }

        public Memory<byte> Reserve(int size, Guid messageId)
        {
            var memoryOwner = MemoryPool<byte>.Shared.Rent(size);
            _messageMemoryMap[messageId] = memoryOwner;
            return memoryOwner.Memory;
        }
    }
}
