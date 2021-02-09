using System;
using System.Buffers;
using MessageBroker.Common.Binary;
using MessageBroker.Models;

namespace MessageBroker.Serialization
{
    public class SerializedPayload
    {
        private byte[] _buffer;
        private int _size;
        
        public PayloadType Type { get; private set; }
        public Guid Id { get; private set; }
        
        public Memory<byte> Data => _buffer.AsMemory(0, _size);
        public Memory<byte> DataWithoutSize => Data.Slice(BinaryProtocolConfiguration.PayloadHeaderSize);

        public void FillFrom(byte[] data, int size, Guid id, PayloadType type)
        {
            if ((_buffer?.Length ?? 0) < size)
            {
                if (_buffer != null)
                    ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(size);
            }

            data.AsMemory(0, size).CopyTo(_buffer.AsMemory());
            _size = size;

            Type = type;
            Id = id;
        }

    }
}