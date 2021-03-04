using System;
using System.Buffers;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Common.Binary
{
    public class BinaryPayload: IPooledObject
    {
        private byte[] _data;
        private int _size;

        public Guid PoolId { get; }
        public bool IsReturnedToPool { get; private set; }

        public BinaryPayload()
        {
            PoolId = Guid.NewGuid();
        }

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

        public void SetPooledStatus(bool isReturned)
        {
            IsReturnedToPool = isReturned;
        }
    }
}