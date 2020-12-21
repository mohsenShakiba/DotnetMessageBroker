using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    public interface IPayload
    {
        bool IsSending { get; }
        bool IsReceiving { get; }

        void DisposeWhenSent();
        void DisposeWhenReceived();

        IMemoryOwner<byte> MemoryOwner { get; }
    }
}
