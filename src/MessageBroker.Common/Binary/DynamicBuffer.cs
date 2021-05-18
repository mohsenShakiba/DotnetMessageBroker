using System;
using System.Buffers;

namespace MessageBroker.Common.Binary
{
    /// <summary>
    /// Provides a dynamically-sized buffer that efficiently provides reading and writing of binary data
    /// used by <see cref="BinaryDataProcessor" /> to process incoming payloads
    /// </summary>
    public class DynamicBuffer : IDisposable
    {
        private byte[] _buffer;
        private int _end;
        private int _start;

        public DynamicBuffer()
        {
            _buffer = ArrayPool<byte>.Shared.Rent(1024);
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer, true);
        }

        public void Write(Memory<byte> m)
        {
            lock (this)
            {
                ReorganizeBufferIfNeeded();

                var remainingSize = _buffer.Length - _end;

                if (remainingSize < m.Length) AllocateNewBufferWithExtraSize(m.Length);

                m.CopyTo(_buffer.AsMemory(_end));
                _end += m.Length;
            }
        }

        public bool CanRead(int size)
        {
            lock (this)
            {
                if (_start + size <= _end) return true;

                return false;
            }
        }

        public Span<byte> ReadAndClear(int size)
        {
            lock (this)
            {
                if (_start + size > _end)
                    throw new InvalidOperationException();

                var m = _buffer.AsSpan(_start, size);
                _start += size;


                return m;
            }
        }

        public Span<byte> Read(int size)
        {
            lock (this)
            {
                if (_start + size > _end)
                    throw new InvalidOperationException();

                var m = _buffer.AsSpan(_start, size);

                return m;
            }
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
            ArrayPool<byte>.Shared.Return(_buffer, true);
            _buffer = newBuffer;
            _end = _end - _start;
            _start = 0;
        }
    }
}