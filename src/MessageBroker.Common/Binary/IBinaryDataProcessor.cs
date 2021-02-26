using System;

namespace MessageBroker.Common.Binary
{
    public interface IBinaryDataProcessor
    {
        public bool Debug { get; set; }
        void Write(Memory<byte> chunk);
        bool TryRead(out BinaryPayload binaryPayload);
    }
}