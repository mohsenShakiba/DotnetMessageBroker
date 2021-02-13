using System;
using System.Buffers;

namespace MessageBroker.Common.Binary
{
    public class BinaryPayload
    {
        private byte[] _data;

        private int _size;

        public Memory<byte> DataWithoutSize => _data.AsMemory(BinaryProtocolConfiguration.PayloadHeaderSize,
            _size - BinaryProtocolConfiguration.PayloadHeaderSize);


        public void Setup(byte[] data, int size)
        {
            _data = data;
            _size = size;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_data);
        }
    }
}