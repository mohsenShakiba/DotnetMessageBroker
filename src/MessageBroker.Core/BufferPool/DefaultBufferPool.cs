using Microsoft.Extensions.ObjectPool;
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

        private readonly ConcurrentBag<SendPayload> _sendPayloadPool;
        public DefaultBufferPool()
        {
            _sendPayloadPool = new();
        }

        public SendPayload RendSendPayload()
        {
            if (_sendPayloadPool.TryTake(out var sp))
            {
                return sp;
            }
            else
            {
                return new SendPayload();
            }
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

        public void ReturnSendPayload(SendPayload payload)
        {
            //if (_sendPayloadPool.Count > 128)
            //{
            //    return;
            //}
            //else
            //{
                _sendPayloadPool.Add(payload);
            //}
        }
    }
}
