using System;
using System.Buffers;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Common.Serialization
{
    /// <summary>
    /// A utility class to that provides helper methods to deserialize binary to payload
    /// </summary>
    public class BinaryProtocolReader : IPooledObject
    {
        /// <summary>
        /// Offset of receive data buffer that data was read from
        /// further reading will start from this offset
        /// </summary>
        private int _currentOffset;

        /// <summary>
        /// received data buffer
        /// </summary>
        private Memory<byte> _receivedData;

        public Guid PoolId { get; set; }


        /// <summary>
        /// Will set the current offset and data buffer
        /// </summary>
        /// <param name="data">Buffer for received data</param>
        public void Setup(Memory<byte> data)
        {
            // skip the type 
            _currentOffset = BinaryProtocolConfiguration.SizeForInt + BinaryProtocolConfiguration.SizeForNewLine;
            _receivedData = data;
        }

        /// <summary>
        /// Will read a Guid from buffer with the current offset
        /// advance the current offset by the size of a Guid
        /// </summary>
        /// <returns>Guid from buffer</returns>
        public Guid ReadNextGuid()
        {
            try
            {
                var data = _receivedData.Span.Slice(_currentOffset, BinaryProtocolConfiguration.SizeForGuid);
                _currentOffset += BinaryProtocolConfiguration.SizeForGuid + BinaryProtocolConfiguration.SizeForNewLine;
                return new Guid(data);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException(
                    $"Cannot read Guid, current is {_currentOffset}, needed is {BinaryProtocolConfiguration.SizeForGuid}, available is {_receivedData.Length - _currentOffset}");
            }
        }

        /// <summary>
        /// Will read a string from buffer with the current offset
        /// advance the current offset by the size of a Guid
        /// </summary>
        /// <returns>string from buffer</returns>
        public string ReadNextString()
        {
            try
            {
                var data = _receivedData.Span[_currentOffset..];
                var indexOfDelimiter = data.IndexOf((byte) '\n');
                _currentOffset += indexOfDelimiter + 1;
                return StringPool.Shared.GetStringForBytes(data[..indexOfDelimiter]);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException(
                    $"Cannot read string, current is {_currentOffset}, available is {_receivedData.Length - _currentOffset}");
            }
        }

        /// <summary>
        /// Will read an int from buffer with the current offset
        /// advance the current offset by the size of a Guid
        /// </summary>
        /// <returns>int from buffer</returns>
        public int ReadNextInt()
        {
            var data = _receivedData.Span.Slice(_currentOffset, BinaryProtocolConfiguration.SizeForInt);
            _currentOffset += BinaryProtocolConfiguration.SizeForInt + BinaryProtocolConfiguration.SizeForNewLine;
            return BitConverter.ToInt32(data);
        }

        /// <summary>
        /// Will read byte array from buffer with the current offset
        /// advance the current offset by the size of a Guid
        /// </summary>
        /// <returns>byte array from buffer and the size of byte array filled with data</returns>
        public (byte[] OriginalData, int Size) ReadNextBytes()
        {
            try
            {
                var spanForSizeOfBinaryData =
                    _receivedData.Span.Slice(_currentOffset, BinaryProtocolConfiguration.SizeForInt);

                var sizeToRead = BitConverter.ToInt32(spanForSizeOfBinaryData);

                _currentOffset += BinaryProtocolConfiguration.SizeForInt;

                var data = _receivedData.Span.Slice(_currentOffset, sizeToRead);

                _currentOffset += sizeToRead + 1;

                var arr = ArrayPool<byte>.Shared.Rent(sizeToRead);

                data.CopyTo(arr);

                return (arr, sizeToRead);
            }
            catch (Exception)
            {
                throw new ArgumentOutOfRangeException(
                    $"Cannot read bytes, current is {_currentOffset}, available is {_receivedData.Length - _currentOffset}");
            }
        }
    }
}