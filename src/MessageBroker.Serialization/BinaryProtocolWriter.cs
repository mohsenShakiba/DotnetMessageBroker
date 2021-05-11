using System;
using System.Buffers;
using System.Text;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.TCP.Binary;

namespace MessageBroker.Serialization
{
    /// <summary>
    /// A utility class that provides method for writing base types to payload 
    /// </summary>
    public class BinaryProtocolWriter: IDisposable, IPooledObject
    {
        
        public Guid PoolId { get; set; }
        
        /// <summary>
        /// Buffer used for writing data to
        /// </summary>
        private byte[] _buffer;
        
        /// <summary>
        /// The offset of buffer that data is written to
        /// </summary>
        private int _currentBufferOffset;
        
        /// <summary>
        /// Type of payload
        /// </summary>
        private PayloadType _type;
        
        /// <summary>
        /// id of payload 
        /// </summary>
        private Guid _id;


        public BinaryProtocolWriter()
        {
            if (_buffer is null)
                _buffer = ArrayPool<byte>.Shared.Rent(64);
            
            Refresh();
        }

        /// <summary>
        /// Writes the type of payload
        /// </summary>
        /// <param name="type">Type of payload</param>
        /// <returns>returns writer</returns>
        public BinaryProtocolWriter WriteType(PayloadType type)
        {
            _type = type;
            return WriteInt((int) type);
        }

        /// <summary>
        /// Writes the identifier of the payload
        /// </summary>
        /// <param name="id">Identifier of the payload</param>
        /// <returns>returns writer</returns>
        public BinaryProtocolWriter WriteId(Guid id)
        {
            _id = id;
            
            const int requiredSizeForGuid = 16;
            
            MakeSureBufferSizeHasRoomForSize(requiredSizeForGuid);
            
            id.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset));
            
            _currentBufferOffset += requiredSizeForGuid;
            
            WriteNewLine();
            
            return this;
        }

        /// <summary>
        /// Writes an integer to payload
        /// </summary>
        /// <param name="i">An integer</param>
        /// <returns>returns writer</returns>
        public BinaryProtocolWriter WriteInt(int i)
        {
            const int requiredSizeForInt = 4;
            
            MakeSureBufferSizeHasRoomForSize(requiredSizeForInt);
            
            BitConverter.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset), i);
            
            _currentBufferOffset += requiredSizeForInt;
            
            WriteNewLine();
            
            return this;
        }

        /// <summary>
        /// Writes string to payload
        /// </summary>
        /// <param name="s">An string</param>
        /// <returns>returns writer</returns>
        public BinaryProtocolWriter WriteStr(string s)
        {
            // note: we only check for ascii because strings used for queues can only be ascii
            
            MakeSureBufferSizeHasRoomForSize(s.Length);
            
            Encoding.UTF8.GetBytes(s, _buffer.AsSpan(_currentBufferOffset));
            
            _currentBufferOffset += s.Length;

            WriteNewLine();
            
            return this;
        }

        /// <summary>
        /// Writes binary data to payload
        /// </summary>
        /// <param name="m">A memory segment</param>
        /// <returns>returns writer</returns>
        public BinaryProtocolWriter WriteMemory(Memory<byte> m)
        {
            MakeSureBufferSizeHasRoomForSize(m.Length + BinaryProtocolConfiguration.SizeForInt);
            
            BitConverter.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset), m.Length);
            
            m.CopyTo(_buffer.AsMemory(_currentBufferOffset + BinaryProtocolConfiguration.SizeForInt));

            _currentBufferOffset += m.Length + BinaryProtocolConfiguration.SizeForInt;
            
            WriteNewLine();
            
            return this;
        }

        /// <summary>
        /// Returns a pooled SerializedPayload with containing the written data
        /// then refreshes the writer 
        /// </summary>
        /// <returns></returns>
        public SerializedPayload ToSerializedPayload()
        {
            try
            {
                WriteSizeOfPayloadToBufferHeader();
                
                var serializedPayload = ObjectPool.Shared.Rent<SerializedPayload>();
                
                serializedPayload.FillFrom(_buffer, _currentBufferOffset, _id);
            
                return serializedPayload;
            }
            finally
            {
                Refresh();
            }
        }

        private void WriteSizeOfPayloadToBufferHeader()
        {
            var sizeOfPayload = _currentBufferOffset - BinaryProtocolConfiguration.PayloadHeaderSize;
            BitConverter.TryWriteBytes(_buffer, sizeOfPayload);
        }

        private void WriteNewLine()
        {
            BitConverter.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset), '\n');
            _currentBufferOffset += 1;
        }

        private void Refresh()
        {
            _currentBufferOffset = BinaryProtocolConfiguration.PayloadHeaderSize;
        }

        private void MakeSureBufferSizeHasRoomForSize(int s)
        {
            // + 1 for new line `\n`
            var exceedingSize = s - (_buffer.Length - _currentBufferOffset) + 1;

            if (exceedingSize > 0)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length + exceedingSize);
                _buffer.CopyTo(newBuffer.AsMemory());
                ArrayPool<byte>.Shared.Return(_buffer, true);
                _buffer = newBuffer;
            }
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer, true);
        }
    }
}