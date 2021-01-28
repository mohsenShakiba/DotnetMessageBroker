using System;
using System.Buffers;
using MessageBroker.Models;

namespace MessageBroker.Serialization
{
    public class SendPayload : IDisposable
    {
        private byte[] _buffer;
        private int _size;

        public Memory<byte> Data => _buffer.AsMemory(0, _size);

        public Memory<byte> DataWithoutSize =>
            _buffer.AsMemory(SerializationConfig.PayloadHeaderSize, _size - SerializationConfig.PayloadHeaderSize);

        public Guid Id { get; private set; }

        private PayloadType _type;

        public bool IsMessageType => _type == PayloadType.Msg;
        public byte[] Buffer => _buffer;
        public PayloadType Type => _type;

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }

        public void FillFrom(byte[] data, int size, Guid id, PayloadType type)
        {
            if ((_buffer?.Length ?? 0) < size)
            {
                if (_buffer != null)
                    ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(size);
            }

            data.AsMemory(0, size).CopyTo(_buffer.AsMemory());

            Id = id;
            _type = type;
            _size = size;
        }
    }
}