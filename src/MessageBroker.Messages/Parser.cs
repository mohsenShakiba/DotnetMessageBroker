using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Messages
{
    public class Parser
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
        public object Parse(Span<byte> payload)
        {
            var indexOfMessageType = payload.IndexOf(Delimiter);
            var messasgeType = payload.Slice(0, indexOfMessageType);
            var messageBody = payload.Slice(indexOfMessageType + Delimiter.Length);

            var messageTypeStr = Encoding.ASCII.GetString(messasgeType);

            return messageTypeStr switch
            {
                MessageTypes.Ack => ToAck(messageBody),
                MessageTypes.Nack => ToNack(messageBody),
                MessageTypes.Message => ToMessage(messageBody),
                MessageTypes.ListenRoute => null,
                MessageTypes.UnlistenListenRoute => null,
                MessageTypes.RegisterPublisher => null,
                MessageTypes.RegisterSubscriber => null,
                MessageTypes.UnRegister => null,
                _ => null
            };
        }

        private Ack ToAck(Span<byte> data)
        {
            var dataTrimmed = data.TrimEnd(Delimiter);
            var messageId = Guid.Parse(Encoding.UTF8.GetString(dataTrimmed));
            return new Ack(messageId);
        }

        private Nack ToNack(Span<byte> data)
        {
            var dataTrimmed = data.TrimEnd(Delimiter);
            var messageId = Guid.Parse(Encoding.UTF8.GetString(dataTrimmed));
            return new Nack(messageId);
        }

        private Message ToMessage(Span<byte> data)
        {
            var parts = Split(data.ToArray())
                .ToList();

            if (parts.Count != 3)
                throw new InvalidDataException();

            var messageId = Guid.Parse(Encoding.UTF8.GetString(parts[0].ToArray()));
            var route = Encoding.UTF8.GetString(parts[0].ToArray());
            var payload = parts[2];

            return new Message(messageId, route, payload.ToArray());
        }
        private Liste ToListenRoute(Span<byte> data)
        {
            var parts = Split(data.ToArray())
                .ToList();

            if (parts.Count != 3)
                throw new InvalidDataException();

            var messageId = Guid.Parse(Encoding.UTF8.GetString(parts[0].ToArray()));
            var route = Encoding.UTF8.GetString(parts[0].ToArray());
            var payload = parts[2];

            return new Message(messageId, route, payload.ToArray());
        }


        private IEnumerable<Memory<byte>> Split(Memory<byte> b)
        {
            var start = 0;
            while(true)
            {
                var index = b.Span.IndexOf(Delimiter);
                if (index > 0 && index != b.Length)
                {
                    yield return b.Slice(start, index);
                }
                else
                {
                    yield break;
                }
                start = index + Delimiter.Length;
            }
        }
    }
}
