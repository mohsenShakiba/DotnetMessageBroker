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

        private static byte[] _delimiter;

        public static Span<byte> Delimiter
        {
            get
            {
                if (_delimiter != null)
                {
                    return _delimiter.AsSpan();
                }

                var delimiter = "\0";
                var delimiterB = Encoding.ASCII.GetBytes(delimiter);
                _delimiter = delimiterB;
                return delimiterB;
            }
        }

        #region Serialize


        public SendPayload ToSendPayload(Ack ack)
        {
            var payloadSize = 4 + 4 + 1 + 16 + 1;
            var memoryOwner = _bufferPool.Rent(payloadSize);
            var bufferSpan = memoryOwner.Memory.Span;

            // write payload size
            BitConverter.TryWriteBytes(bufferSpan.Slice(0, 4), payloadSize);

            // write payload type
            BitConverter.TryWriteBytes(bufferSpan.Slice(4, 4), (int)PayloadType.Ack);

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(8, 1), '\n');

            // write id
            ack.Id.TryWriteBytes(bufferSpan.Slice(9, 16));

            return new SendPayload
            {
                Id = ack.Id,
                Data = memoryOwner.Memory.Slice(0, payloadSize),
                MemoryOwner = memoryOwner
            };
        }

        public SendPayload ToSendPayload(Message msg)
        {
            var payloadSize = 4 + 4 + 1 + 16 + 1 + msg.Route.Length + 1 + msg.Data.Length + 1;
            var memoryOwner = _bufferPool.Rent(payloadSize);
            var bufferSpan = memoryOwner.Memory.Span;

            // write payload size
            BitConverter.TryWriteBytes(bufferSpan.Slice(0, 4), payloadSize);

            // write payload type
            BitConverter.TryWriteBytes(bufferSpan.Slice(4, 4), (int)PayloadType.Msg);

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(8, 1), '\n');

            // write id
            msg.Id.TryWriteBytes(bufferSpan.Slice(9, 16));

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(25, 1), '\n');

            // write the route
            var routeB = Encoding.UTF8.GetBytes(msg.Route);
            routeB.CopyTo(bufferSpan.Slice(26, routeB.Length));

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(26 + routeB.Length, 1), '\n');

            // write data 
            msg.Data.CopyTo(memoryOwner.Memory.Slice(26 + routeB.Length + 1, msg.Data.Length));

            return new SendPayload
            {
                Id = msg.Id,
                Data = memoryOwner.Memory.Slice(0, payloadSize),
                MemoryOwner = memoryOwner
            };

        }

        public SendPayload ToSendPayload(Subscribe sub)
        {
            var payloadSize = 4 + 4 + 1 + 16 + 1 + 4 + 1;
            var memoryOwner = _bufferPool.Rent(payloadSize);
            var bufferSpan = memoryOwner.Memory.Span;

            // write payload size
            BitConverter.TryWriteBytes(bufferSpan.Slice(0, 4), payloadSize);

            // write payload type
            BitConverter.TryWriteBytes(bufferSpan.Slice(4, 4), (int)PayloadType.Subscribe);

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(8, 1), '\n');

            // write id
            sub.Id.TryWriteBytes(bufferSpan.Slice(9, 16));

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(25, 1), '\n');

            // write concurrency
            BitConverter.TryWriteBytes(bufferSpan.Slice(26, 4), sub.Concurrency);

            return new SendPayload
            {
                Id = sub.Id,
                Data = memoryOwner.Memory.Slice(0, payloadSize),
                MemoryOwner = memoryOwner
            };

        }

        public SendPayload ToSendPayload(Listen listen)
        {
            var payloadSize = 4 + 4 + 1 + 16 + 1 + listen.Route.Length + 1;
            var memoryOwner = _bufferPool.Rent(payloadSize);
            var bufferSpan = memoryOwner.Memory.Span;

            // write payload size
            BitConverter.TryWriteBytes(bufferSpan.Slice(0, 4), payloadSize);

            // write payload type
            BitConverter.TryWriteBytes(bufferSpan.Slice(4, 4), (int)PayloadType.Listen);

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(8, 1), '\n');

            // write id
            listen.Id.TryWriteBytes(bufferSpan.Slice(9, 16));

            // write separator
            BitConverter.TryWriteBytes(bufferSpan.Slice(25, 1), '\n');

            // write route
            var routeB = Encoding.UTF8.GetBytes(listen.Route);
            routeB.CopyTo(bufferSpan.Slice(26));

            return new SendPayload
            {
                Id = listen.Id,
                Data = memoryOwner.Memory.Slice(0, payloadSize),
                MemoryOwner = memoryOwner
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
            var messageId = new Guid(data.Slice(5, 16));
            return new Ack { Id = messageId };
        }


        public Message ToMessage(Span<byte> data)
        {
            try
            {
                data = data.Slice(5);

                var messageId = new Guid(data.Slice(0, 16));
                var indexOfRouteDelimiter = data.Slice(17).IndexOf(Delimiter);
                var route = Encoding.UTF8.GetString(data.Slice(17, indexOfRouteDelimiter));

                var messageMemoryOwner = _bufferPool.Rent(data.Length - (18 + indexOfRouteDelimiter));

                data.Slice(18 + indexOfRouteDelimiter).CopyTo(messageMemoryOwner.Memory.Span);

                return new Message
                {

                    Id = messageId,
                    Route = route,
                    Data = messageMemoryOwner.Memory,
                    OriginalMessageMemoryOwner = messageMemoryOwner
                };
            }
            catch
            {
                throw;
            }
            
        }

        public Listen ToListenRoute(Span<byte> data)
        {
            data = data.Slice(5);
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
            data = data.Slice(5);
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
