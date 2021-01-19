using System;
using System.Buffers;
using System.Text;
using MessageBroker.Core.Configurations;
using MessageBroker.Core.Payloads;
using MessageBroker.Core.Pools;

namespace MessageBroker.Core.Serialize
{
    /// <summary>
    /// BinarySerializeHelper is a utility class that provides method for serialize a payload to binary 
    /// </summary>
    public class BinarySerializeHelper : IDisposable
    {
        private byte[] _buffer;
        private int _currentBufferOffset;
        private Guid _id;
        private PayloadType _type;

        public BinarySerializeHelper WriteType(PayloadType type)
        {
            _type = type;
            return WriteInt((int) type);
        }

        public BinarySerializeHelper WriteId(Guid id)
        {
            _id = id;
            const int requiredSizeForGuid = 16 + 1;
            MakeSureBufferSizeHasRoomForSize(requiredSizeForGuid);
            var bufferSpan = _buffer.AsSpan(_currentBufferOffset);
            id.TryWriteBytes(bufferSpan);
            BitConverter.TryWriteBytes(bufferSpan.Slice(16), '\n');
            _currentBufferOffset += requiredSizeForGuid;
            return this;
        }

        public BinarySerializeHelper WriteInt(int i)
        {
            const int requiredSizeForInt = 4 + 1;
            MakeSureBufferSizeHasRoomForSize(requiredSizeForInt);
            var bufferSpan = _buffer.AsSpan(_currentBufferOffset);
            BitConverter.TryWriteBytes(bufferSpan, i);
            BitConverter.TryWriteBytes(bufferSpan.Slice(4), '\n');
            _currentBufferOffset += requiredSizeForInt;
            return this;
        }

        public BinarySerializeHelper WriteStr(string s)
        {
            var length = s.Length;
            MakeSureBufferSizeHasRoomForSize(length + 1);
            var bufferSpan = _buffer.AsSpan(_currentBufferOffset);
            Encoding.UTF8.GetBytes(s, bufferSpan);
            BitConverter.TryWriteBytes(bufferSpan.Slice(length), '\n');
            _currentBufferOffset += length + 1;
            return this;
        }

        public BinarySerializeHelper WriteMemory(Memory<byte> m)
        {
            var length = m.Length;
            MakeSureBufferSizeHasRoomForSize(length + 1);
            var bufferSpan = _buffer.AsSpan(_currentBufferOffset);
            m.Span.CopyTo(bufferSpan);
            BitConverter.TryWriteBytes(bufferSpan.Slice(length), '\n');
            _currentBufferOffset += length + 1;
            return this;
        }

        public SendPayload Build()
        {
            try
            {
                var bufferSpan = _buffer.AsSpan();
                var headerSize = ConfigurationProvider.Shared.BaseConfiguration.MessageHeaderSize;
                BitConverter.TryWriteBytes(bufferSpan, _currentBufferOffset - headerSize);

                var sendPayload = ObjectPool.Shared.RentSendPayload();
                sendPayload.FillFrom(_buffer, _currentBufferOffset, _id, _type);

                return sendPayload;
            }
            finally
            {
                ObjectPool.Shared.Return(this);
            }
        }

        public void Refresh()
        {
            var baseConfiguration = ConfigurationProvider.Shared.BaseConfiguration;
            _currentBufferOffset = baseConfiguration.MessageHeaderSize;
        }


        public void Setup()
        {
            var baseConfiguration = ConfigurationProvider.Shared.BaseConfiguration;

            if (_buffer == null)
            {
                _buffer = ArrayPool<byte>.Shared.Rent(baseConfiguration.StartMessageSize);
            }

            _currentBufferOffset = baseConfiguration.MessageHeaderSize;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }

        private void MakeSureBufferSizeHasRoomForSize(int s)
        {
            var exceedingSize = s - (_buffer.Length - _currentBufferOffset);

            if (exceedingSize > 0)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length + exceedingSize);
                _buffer.CopyTo(newBuffer.AsMemory());
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
        }
    }
}