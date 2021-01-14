using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.BufferPool
{
    public class ReceivePayload
    {
        private static byte[] _delimiter;

        public static Span<byte> Delimiter
        {
            get
            {
                if (_delimiter != null)
                {
                    return _delimiter.AsSpan();
                }

                var delimiter = "\n";
                var delimiterB = Encoding.ASCII.GetBytes(delimiter);
                _delimiter = delimiterB;
                return delimiterB;
            }
        }

        private Memory<byte> _receivedData;
        private int _currentOffset = 0;

        public void Setup(Memory<byte> data)
        {
            // ignore type and size
            _currentOffset = 5;
            _receivedData = data;
        }

        public Guid ReadNextGuid()
        {
            var data = _receivedData.Span.Slice(_currentOffset, 16);
            var guid = new Guid(data);
            _currentOffset += 16 + 1;
            return guid;
        }

        public string ReadNextString()
        {
            var data = _receivedData.Span.Slice(_currentOffset);
            var indexOfDelimiter = data.IndexOf(Delimiter);

        }




    }
}
