using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public interface IBufferPool
    {
        byte[] Rent(int size);
        void Return(byte[] data);
    }
}
