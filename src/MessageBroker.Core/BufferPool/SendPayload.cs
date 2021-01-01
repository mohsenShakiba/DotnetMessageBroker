using MessageBroker.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public class SendPayload

    {
        private byte[] _buffer;
        private int _currentBufferOffset;
        private IBufferPool _bufferPool;

        public Guid Id { get; }
        public Memory<byte> Data => _buffer.AsMemory(0, _currentBufferOffset);
        public Memory<byte> DataWithoutSize => _buffer.AsMemory(4, _currentBufferOffset - 4);


        public SendPayload(IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
        }

        public SendPayload WriteType(PayloadType type)
        {
            return WriteInt((int)type);
        }

        public SendPayload WriteId(Guid id)
        {
            MakeSureBufferSizeHasRoomForSize(17);
            var bufferSpan = _buffer.AsSpan(_currentBufferOffset);
            id.TryWriteBytes(bufferSpan);
            BitConverter.TryWriteBytes(bufferSpan.Slice(16), '\n');
            _currentBufferOffset += 17;
            return this;
        }

        public SendPayload WriteInt(int i)
        {
            MakeSureBufferSizeHasRoomForSize(5);
            var bufferSpan = _buffer.AsSpan(_currentBufferOffset);
            BitConverter.TryWriteBytes(bufferSpan, i);
            BitConverter.TryWriteBytes(bufferSpan.Slice(4), '\n');
            _currentBufferOffset += 5;
            return this;
        }

        public SendPayload WriteStr(string s)
        {
            var stringBytes = Encoding.UTF8.GetBytes(s);
            var length = stringBytes.Length;
            MakeSureBufferSizeHasRoomForSize(length + 1);
            var bufferSpan = _buffer.AsSpan(_currentBufferOffset);
            stringBytes.CopyTo(bufferSpan);
            BitConverter.TryWriteBytes(bufferSpan.Slice(length), '\n');
            _currentBufferOffset += length + 1;
            return this;
        }

        public SendPayload WriteMemory(Memory<byte> m)
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
            var bufferSpan = _buffer.AsSpan();
            BitConverter.TryWriteBytes(bufferSpan, _currentBufferOffset - 4);
            return this;
        }


        public void Setup()
        {
            _buffer = _bufferPool.Rent(1024);
            _currentBufferOffset = 4;
        }

        public void Refresh()
        {
            _currentBufferOffset = 4;
        }

        private void MakeSureBufferSizeHasRoomForSize(int s)
        {
            var exceedingSize = s - (_buffer.Length - _currentBufferOffset);

            if (exceedingSize > 0)
            {
                var newBuffer = _bufferPool.Rent(_buffer.Length + exceedingSize);
                _buffer.CopyTo(newBuffer.AsMemory());
                _bufferPool.Return(_buffer);
                _buffer = newBuffer;
            } 
        }

    }

}
