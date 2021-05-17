using System;
using System.Buffers;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Models.Binary
{
    
    /// <summary>
    /// Contains a ready to send binary payload that also exposes the identifier of the payload
    /// sice the identifier of the payload is used for keeping track of status of payload
    /// </summary>
    public class SerializedPayload: IPooledObject
    {
        
        private byte[] _buffer;
        private int _size;
        
        /// <summary>
        /// Identifier of the payload, this value is needed for various purposes including to keep track of sending status
        /// </summary>
        public Guid PayloadId { get; private set; }
        
        /// <summary>
        /// Identifier of the object tracked by the <see cref="ObjectPool"/>
        /// </summary>
        public Guid PoolId { get; set; }

        /// <summary>
        /// Data to be send on wire
        /// </summary>
        public Memory<byte> Data => _buffer.AsMemory(0, _size);
        
        /// <summary>
        /// Same as Data but without the header size
        /// used for testing mostly
        /// </summary>
        public Memory<byte> DataWithoutSize => Data[BinaryProtocolConfiguration.PayloadHeaderSize..];


        /// <summary>
        /// Will set the binary payload
        /// </summary>
        /// <param name="data">Data to be sent</param>
        /// <param name="size">Size of data</param>
        /// <param name="id">Identifier of the payload</param>
        public void FillFrom(byte[] data, int size, Guid id)
        {
            if ((_buffer?.Length ?? 0) < size)
            {
                if (_buffer != null)
                    ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(size);
            }

            data.AsMemory(0, size).CopyTo(_buffer.AsMemory());
            _size = size;

            PayloadId = id;
        }

    }
}