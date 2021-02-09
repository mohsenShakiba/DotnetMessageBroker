using System;
using System.Buffers;
using MessageBroker.Common.Pooling;
using MessageBroker.Common.Utils;

namespace MessageBroker.Common.Binary
{
    public class BinaryDataProcessor : IBinaryDataProcessor
    {
        private readonly DynamicBuffer _dynamicBuffer;
        private readonly object _lock;

        public BinaryDataProcessor()
        {
            _dynamicBuffer = new();
            _lock = new();
        }

        public void Write(Memory<byte> chunk)
        {
            lock (_lock)
            {
                _dynamicBuffer.Write(chunk);
            }
        }

        public bool TryRead(out BinaryPayload binaryPayload)
        {
            lock (_lock)
            {
                binaryPayload = null;
                var canReadHeaderSize = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize);

                if (!canReadHeaderSize)
                    return false;

                var headerSizeBytes = _dynamicBuffer.Read(BinaryProtocolConfiguration.PayloadHeaderSize);
                var headerSize = BitConverter.ToInt32(headerSizeBytes);

                var canReadPayload = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

                if (!canReadPayload)
                    return false;

                var payload = _dynamicBuffer.ReadAndClear(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

                var receiveDataBuffer = ArrayPool<byte>.Shared.Rent(payload.Length);
                
                payload.CopyTo(receiveDataBuffer);

                binaryPayload = ObjectPool.Shared.Rent<BinaryPayload>();
                binaryPayload.Setup(receiveDataBuffer, payload.Length);

                return true;
            }
        }
    }
}