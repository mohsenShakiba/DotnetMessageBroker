using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using Serilog.Events;

namespace MessageBroker.Common.Binary
{
    /// <inheritdoc />
    public class BinaryDataProcessor : IBinaryDataProcessor
    {
        private readonly DynamicBuffer _dynamicBuffer;
        private bool _disposed;
        private bool _isReading;

        public BinaryDataProcessor()
        {
            _dynamicBuffer = new DynamicBuffer();
        }

        public void Write(Memory<byte> chunk)
        {
            _dynamicBuffer.Write(chunk);
        }

        public void BeginLock()
        {
            _isReading = true;
        }

        public void EndLock()
        {
            _isReading = false;
        }

        public bool TryRead(out BinaryPayload binaryPayload)
        {
            // we need to use lock for this method
            // because the dispose might be called in the middle of this method
            // and cause unexpected results
            lock (_dynamicBuffer)
            {
                binaryPayload = null;
                
                if (_disposed)
                {
                    return false;
                }
                
                var canReadHeaderSize = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize);

                if (!canReadHeaderSize)
                {
                    return false;
                }

                var headerSizeBytes = _dynamicBuffer.Read(BinaryProtocolConfiguration.PayloadHeaderSize);
                var headerSize = BitConverter.ToInt32(headerSizeBytes);

                var canReadPayload = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

                if (!canReadPayload)
                {
                    return false;
                }

                var payload = _dynamicBuffer.ReadAndClear(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

                var receiveDataBuffer = ArrayPool<byte>.Shared.Rent(payload.Length);

                payload.CopyTo(receiveDataBuffer);

                binaryPayload = ObjectPool.Shared.Rent<BinaryPayload>();


                binaryPayload.Setup(receiveDataBuffer, payload.Length);

                return true;
            }
            
        }
        
        public void Dispose()
        {
            while (_isReading)
            {
                Thread.Yield();
            }
            
            if (_disposed)
            {
                return;
            }
            
            // so that disposing the buffer would not interfere with TreRead method
            lock (_dynamicBuffer)
            {
                _disposed = true;
                _dynamicBuffer.Dispose();
            }
        }
    }
}