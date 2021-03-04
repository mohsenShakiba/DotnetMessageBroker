using System;
using System.Buffers;
using System.Text;
using MessageBroker.Common.Pooling;
using MessageBroker.Serialization.Pools;

namespace MessageBroker.Serialization
{
    /// <summary>
    ///     BinaryDeserializeHelper is a utility class to that provides helper methods to deserialize binary to payload
    /// </summary>
    public class BinaryDeserializeHelper: IPooledObject
    {
        private static byte[] _delimiter;
        private int _currentOffset;
        private Memory<byte> _receivedData;
        public Guid PoolId { get; }
        public bool IsReturnedToPool { get; private set; }

        public static Span<byte> Delimiter
        {
            get
            {
                if (_delimiter != null) return _delimiter.AsSpan();

                var delimiter = "\n";
                var delimiterB = Encoding.ASCII.GetBytes(delimiter);
                _delimiter = delimiterB;
                return delimiterB;
            }
        }

        public BinaryDeserializeHelper()
        {
            PoolId = Guid.NewGuid();
        }

        public void Setup(Memory<byte> data)
        {
            _currentOffset = 5;
            _receivedData = data;
        }

        public Guid ReadNextGuid()
        {
            var data = _receivedData.Span.Slice(_currentOffset, 16);
            _currentOffset += 16 + 1;
            return new Guid(data);
        }

        public string ReadNextString()
        {
            var data = _receivedData.Span.Slice(_currentOffset);
            var indexOfDelimiter = data.IndexOf(Delimiter);
            _currentOffset += indexOfDelimiter + 1;
            return StringPool.Shared.GetStringForBytes(data.Slice(0, indexOfDelimiter));
        }

        public int ReadNextInt()
        {
            var data = _receivedData.Span.Slice(_currentOffset, 4);
            _currentOffset += 4 + 1;
            return BitConverter.ToInt32(data);
        }

        public (byte[] OriginalData, int Size) ReadNextBytes()
        {
            var data = _receivedData.Span.Slice(_currentOffset);
            var indexOfDelimiter = data.IndexOf(Delimiter);
            _currentOffset += indexOfDelimiter + 1;
            var arr = ArrayPool<byte>.Shared.Rent(indexOfDelimiter);
            data.Slice(0, indexOfDelimiter).CopyTo(arr);
            return (arr, indexOfDelimiter);
        }

        public void SetPooledStatus(bool isReturned)
        {
            IsReturnedToPool = isReturned;
        }
    }
}