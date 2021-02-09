using System;
using System.Buffers;

namespace MessageBroker.Common.Binary
{
    public class BinaryPayload
    {
        public Memory<byte> DataWithoutSize => _data.AsMemory(BinaryProtocolConfiguration.PayloadHeaderSize, _size - BinaryProtocolConfiguration.PayloadHeaderSize);

        private int _size;
        private byte[] _data;


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