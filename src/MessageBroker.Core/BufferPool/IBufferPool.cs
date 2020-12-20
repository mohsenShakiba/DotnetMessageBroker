using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public interface IBufferPool
    {
        void Release(Guid messageId);
        Memory<byte> Reserve(int size, Guid messageId);
    }
}
