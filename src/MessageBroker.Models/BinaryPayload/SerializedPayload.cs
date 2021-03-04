using System;
using System.Buffers;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Models.BinaryPayload
{
    public class SerializedPayload: IPooledObject
    {
        private byte[] _buffer;
        private int _size;
        
        public event Action<Guid, SerializedPayloadStatusUpdate> OnStatusChanged;

        public PayloadType Type { get; private set; }
        public Guid Id { get; set; }
        public Guid PoolId { get; }
        public bool IsReturnedToPool { get; private set; }

        public Memory<byte> Data => _buffer.AsMemory(0, _size);
        public Memory<byte> DataWithoutSize => Data.Slice(BinaryProtocolConfiguration.PayloadHeaderSize);

        public SerializedPayload()
        {
            PoolId = Guid.NewGuid();
        }

        public void FillFrom(byte[] data, int size, Guid id, PayloadType type)
        {
            if (Id != Guid.Empty)
            {
                Logger.LogInformation($"SerializedPayload -> changing the id from {Id} to {id} {IsReturnedToPool}");
            }
            if ((_buffer?.Length ?? 0) < size)
            {
                if (_buffer != null)
                    ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(size);
            }

            data.AsMemory(0, size).CopyTo(_buffer.AsMemory());
            _size = size;

            Type = type;
            Id = id;
        }

        public void ClearStatusListener()
        {
            OnStatusChanged = null;
        }
        
        public void SetStatus(SerializedPayloadStatusUpdate payloadStatusUpdate)
        {
            OnStatusChanged?.Invoke(Id, payloadStatusUpdate);
        }

        public void SetPooledStatus(bool isReturned)
        {
            if (isReturned && IsReturnedToPool)
            {
                Logger.LogInformation($"SerializedPayload -> invalid reserve ");
            }
            Logger.LogInformation($"SerializedPayload -> Return called for {Id} with {isReturned}");
            IsReturnedToPool = isReturned;
        }
    }
}