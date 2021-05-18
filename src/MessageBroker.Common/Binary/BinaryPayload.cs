using System;
using System.Buffers;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Common.Binary
{
    /// <summary>
    /// Used by <see cref="BinaryDataProcessor" /> to provide access to processed payload, this object will provide
    /// a dispose method to return its buffer to array pool
    /// </summary>
    public class BinaryPayload : IPooledObject
    {
        private byte[] _data;
        private int _size;

        public BinaryPayload()
        {
            PoolId = Guid.NewGuid();
        }

        public Memory<byte> DataWithoutSize => _data.AsMemory(BinaryProtocolConfiguration.PayloadHeaderSize,
            _size - BinaryProtocolConfiguration.PayloadHeaderSize);

        public Guid PoolId { get; set; }


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