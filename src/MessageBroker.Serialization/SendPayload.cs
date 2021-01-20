using System;
using System.Buffers;
using MessageBroker.Models.Models;

namespace MessageBroker.Serialization
{
    public class SendPayload : IDisposable
    {
        private readonly SerializationConfig _config;

        private byte[] _buffer;
        private int _size;


        public SendPayload(SerializationConfig config)
        {
            _config = config;
        }

        public Memory<byte> Data => _buffer.AsMemory(0, _size);

        public Memory<byte> DataWithoutSize =>
            _buffer.AsMemory(_config.MessageHeaderSize, _size - _config.MessageHeaderSize);

        public Guid Id { get; private set; }

        public PayloadType Type { get; private set; }

        public bool IsMessageType => Type == PayloadType.Msg;

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
            Type = type;
            _size = size;
        }
    }
}