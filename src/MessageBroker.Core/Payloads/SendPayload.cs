using System;
using System.Buffers;
using MessageBroker.Core.Configurations;

namespace MessageBroker.Core.Payloads
{
    public class SendPayload: IDisposable
    {
        private byte[] _buffer;
        private int _size;
        private readonly int _defaultMessageSizeLength;
        
        public Memory<byte> Data => _buffer.AsMemory(0, _size);
        public Memory<byte> DataWithoutSize => _buffer.AsMemory(_defaultMessageSizeLength, _size - _defaultMessageSizeLength);

        public SendPayload()
        {
            _defaultMessageSizeLength = ConfigurationProvider.Shared.BaseConfiguration.MessageHeaderSize;
        }

        public void FillFrom(byte[] data, int size)
        {
            if (_buffer.Length < size)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            data.CopyTo(_buffer.AsMemory());
            _size = size;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }
}