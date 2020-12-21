using MessageBroker.Core.BufferPool;
using MessageBroker.Core.Extensions;
using MessageBroker.Core.Models;
using MessageBroker.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Serialize
{
    public class DefaultSerializer : ISerializer
    {

        private readonly IBufferPool _bufferPool;
        private const int GuidSize = 16;
        private const int PayloadTypeSize = 4;

        public DefaultSerializer(IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
        }

        #region Serialize

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

        public SendPayload ToSendPayload(Ack ack)
        {
            var size = GuidSize + PayloadTypeSize;
            var memoryOwner = _bufferPool.Rent(size);

            var span = memoryOwner.Memory.Span.Slice(0, size);

            BitConverter.TryWriteBytes(span, (int)PayloadType.Ack);
            ack.Id.TryWriteBytes(span.Slice(PayloadTypeSize));

            return new SendPayload
            {
                MemoryOwner = memoryOwner,
                Data = memoryOwner.Memory
            };
        }

        /// <summary>
        /// For testing purposes only
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public SendPayload ToSendPayloadTest(Message msg)
        {
            var size = GuidSize + PayloadTypeSize + msg.Route.Length + msg.Data.Length;
            var memoryOwner = _bufferPool.Rent(size);

            var span = memoryOwner.Memory.Span.Slice(0, size);

            BitConverter.TryWriteBytes(span, (int)PayloadType.Nack);
            msg.Id.TryWriteBytes(span.Slice(PayloadTypeSize));
            var routeB = Encoding.UTF8.GetBytes(msg.Route);
            routeB.CopyTo(span.Slice(PayloadTypeSize + GuidSize));
            msg.Data.CopyTo(memoryOwner.Memory.Slice(PayloadTypeSize + GuidSize + routeB.Length));

            return new SendPayload
            {
                MemoryOwner = memoryOwner,
                Data = memoryOwner.Memory
            };
        }

        public SendPayload ToSendPayload(Message msg)
        {
            return new SendPayload
            {
                MemoryOwner = msg.OriginalMessageMemoryOwner,
                Data = msg.OriginalMessageMemoryOwner.Memory
            };

        }

        #endregion

        #region Deserialize

        public PayloadType ParsePayloadType(Memory<byte> b)
        {
            var messageType = (PayloadType)BitConverter.ToInt32(b.Slice(0, 4).Span);
            return messageType;
        }


        public Ack ToAck(Span<byte> data)
        {
            var dataTrimmed = data.TrimEnd(Delimiter);
            var messageId = new Guid(dataTrimmed);
            return new Ack { Id = messageId };
        }


        public Message ToMessage(Span<byte> data)
        {
            var messageMemoryOwner = _bufferPool.Rent(data.Length);

            data.CopyTo(messageMemoryOwner.Memory.Span);

            var messageId = new Guid(data.Slice(0, 16));
            var indexOfRouteDelimiter = data.Slice(17).IndexOf(Delimiter);
            var route = Encoding.UTF8.GetString(data.Slice(17, indexOfRouteDelimiter));

            return new Message
            {

                Id = messageId,
                Route = route,
                OriginalMessageMemoryOwner = messageMemoryOwner
            };
        }

        public Listen ToListenRoute(Span<byte> data)
        {
            var id = new Guid(data.Slice(0, 16));
            var route = Encoding.UTF8.GetString(data.Slice(17, data.Length - 18));

            return new Listen
            {
                Id = id,
                Route = route
            };
        }


        public Subscribe ToSubscribe(Span<byte> data)
        {
            var id = new Guid(data.Slice(0, 16));
            var concurrency = BitConverter.ToInt32(data.Slice(17, 4));

            return new Subscribe
            {
                Id = id,
                Concurrency = concurrency
            };
        }

        #endregion

    }
}
