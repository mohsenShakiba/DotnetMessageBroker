using System;

namespace MessageBroker.Common.Binary
{
    public interface IBinaryDataProcessor
    {
        void Write(Memory<byte> chunk);
        bool TryRead(out BinaryPayload binaryPayload);
    }
}