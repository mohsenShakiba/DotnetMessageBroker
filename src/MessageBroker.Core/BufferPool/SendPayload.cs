using MessageBroker.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public ref struct SendPayload
    {
        public IMemoryOwner<byte> MemoryOwner { get; init; }
        public Memory<byte> Data { get; init; }

        public void Dispose()
        {
            MemoryOwner.Dispose();
        }

    }

    public class SendPayloadBuilder
    {

        private int _bufferSize;
        private int _currentOffset;
        private readonly PayloadType _type;

        public Memory<byte> Data => MemoryOwner.Memory.Slice(0, _currentOffset);

        public IMemoryOwner<byte> MemoryOwner { get; private set; }


        public SendPayloadBuilder(PayloadType type)
        {
            // 22 is the size of a typical message consisting of payload size, payload type and a guid
            _bufferSize = 26;
            _type = type;
        }

        public void WithAdditionalSize(int additionalSize)
        {
            _bufferSize += additionalSize + 1;
        }

        public void InitiateBuffer()
        {
            MemoryOwner = MemoryPool<byte>.Shared.Rent(_bufferSize);
            WriteInt(_bufferSize);
            WriteInt((int)_type);
        }

        public void WriteGuid(Guid id)
        {
            id.TryWriteBytes(MemoryOwner.Memory.Span.Slice(_currentOffset, 16));
            BitConverter.TryWriteBytes(MemoryOwner.Memory.Span.Slice(_currentOffset + 16, 1), '\n');
            _currentOffset += 17;
        }

        public void WriteString(string s)
        {
            Encoding.UTF8.GetBytes(s).CopyTo(MemoryOwner.Memory.Span.Slice(_currentOffset, s.Length));
            BitConverter.TryWriteBytes(MemoryOwner.Memory.Span.Slice(_currentOffset + s.Length, 1), '\n');
            _currentOffset += s.Length + 1;
        }

        public void WriteInt(int i)
        {
            BitConverter.TryWriteBytes(MemoryOwner.Memory.Span.Slice(_currentOffset, 4), i);
            BitConverter.TryWriteBytes(MemoryOwner.Memory.Span.Slice(_currentOffset + 4, 1), '\n');
            _currentOffset += 5;
        }

        public void WriteData(Memory<byte> data)
        {
            data.CopyTo(MemoryOwner.Memory.Slice(_currentOffset, data.Length));
            BitConverter.TryWriteBytes(MemoryOwner.Memory.Span.Slice(_currentOffset + data.Length, 1), '\n');
            _currentOffset += data.Length + 1;
        }
    }
}
