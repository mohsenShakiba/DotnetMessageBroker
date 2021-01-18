using System;
using System.Buffers;
using MessageBroker.Core.Configurations;

namespace MessageBroker.Core.Payloads
{
    public class SendPayload: IDisposable
    {
        private readonly int _defaultMessageSizeLength;
        
        private byte[] _buffer;
        private int _size;
        private Guid _id;
        private PayloadType _type;
        
        public Memory<byte> Data => _buffer.AsMemory(0, _size);
        public Memory<byte> DataWithoutSize => _buffer.AsMemory(_defaultMessageSizeLength, _size - _defaultMessageSizeLength);
        public Guid Id => _id;
        public PayloadType Type => _type;
        public bool IsMessageType => _type == PayloadType.Msg;


        public SendPayload()
        {
            _defaultMessageSizeLength = ConfigurationProvider.Shared.BaseConfiguration.MessageHeaderSize;
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

            _id = id;
            _type = type;
            _size = size;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }

    }
}