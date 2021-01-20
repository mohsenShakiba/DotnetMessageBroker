using System;
using System.Buffers;

namespace MessageBroker.Client.Buffers
{
    public class MemoryBuffer
    {
        private readonly object _lock;
        private byte[] _buffer;
        private int _currentOffset;

        public MemoryBuffer()
        {
            _lock = new object();
        }

        public void Append(byte[] data)
        {
            lock (_lock)
            {
                SetupBufferForSize(data.Length);

                data.CopyTo(_buffer.AsMemory(_currentOffset));
                _currentOffset += data.Length;
            }
        }

        public Memory<byte> GetBytes()
        {
            if (_buffer == null)
                throw new InvalidOperationException();

            lock (_lock)
            {
                var memory = _buffer.AsMemory(0, _currentOffset);
                _currentOffset = 0;
                return memory;
            }
        }

        private void SetupBufferForSize(int size)
        {
            var remainingSize = _buffer.Length - _currentOffset;

            if (remainingSize < size)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length + size);

                if (_buffer != null)
                {
                    _buffer.CopyTo(newBuffer.AsMemory());
                    ArrayPool<byte>.Shared.Return(_buffer);
                }

                _buffer = newBuffer;
            }
        }
    }
}