using System;
using System.Buffers;

namespace MessageBroker.Common.Utils
{
    public class DynamicBuffer
    {
        private int _start;
        private int _end;
        private byte[] _buffer;

        public DynamicBuffer()
        {
            _buffer = ArrayPool<byte>.Shared.Rent(DynamicBufferConfiguration.StartBufferSize);
        }

        public Memory<byte> ReadAndClearAll()
        {
            try
            {
                return _buffer.AsMemory(_start, _end);
            }
            finally
            {
                _start = 0;
                _end = 0;
            }
        }

        public void Write(Memory<byte> m)
        {
            ReorganizeBufferIfNeeded();
            
            var remainingSize = _buffer.Length - _end;

            if (remainingSize >= m.Length)
            {
                m.CopyTo(_buffer.AsMemory(_end));
            }
            else
            {
                AllocateNewBufferWithExtraSize(m.Length);
                m.CopyTo(_buffer.AsMemory(_end));
            }
            _end += m.Length;
        }

        public void SetCurrent(int start)
        {
            _start = start;
            _end = start;
        }

        public bool CanRead(int size)
        {
            if (_start + size <= _end)
            {
                return true;
            }

            return false;
        }

        public Span<byte> ReadAndClear(int size)
        {
            if (_start + size > _end)
                throw new InvalidOperationException();

            var m = _buffer.AsSpan(_start, size);
            _start += size;

            
            return m;
        }
        
        public Span<byte> Read(int size)
        {
            if (_start + size > _end)
                throw new InvalidOperationException();

            var m = _buffer.AsSpan(_start, size);
            
            return m;
        }

        private void ReorganizeBufferIfNeeded()
        {
            if (_start >= _buffer.Length / 2)
            {
                _buffer.AsMemory(_start).CopyTo(_buffer);
                _end -= _start;
                _start = 0;
            }
        }


        private void AllocateNewBufferWithExtraSize(int extraSize)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length + extraSize);
            _buffer.AsMemory(_start).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
            _end = _end - _start;
            _start = 0;
        }
    }
}