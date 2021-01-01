using MessageBroker.Core.BufferPool;
using MessageBroker.Core.Models;
using MessageBroker.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Serialize
{
    public class DefaultSerializer: ISerializer
    {
        private readonly IBufferPool _bufferPool;

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

        public DefaultSerializer(IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
        }

        public SendPayload ToSendPayload(Ack ack)
        {
            var sendPayload = _bufferPool.RendSendPayload();

            return sendPayload
                .WriteType(PayloadType.Ack)
                .WriteId(ack.Id)
                .Build();
        }

        public SendPayload ToSendPayload(Message msg)
        {
            var sendPayload = _bufferPool.RendSendPayload();

            return sendPayload
                .WriteType(PayloadType.Msg)
                .WriteId(msg.Id)
                .WriteStr(msg.Route)
                .WriteMemory(msg.Data)
                .Build();
        }

        public SendPayload ToSendPayload(Subscribe subscribe)
        {
            var sendPayload = _bufferPool.RendSendPayload();

            return sendPayload
                .WriteType(PayloadType.Subscribe)
                .WriteId(subscribe.Id)
                .WriteInt(subscribe.Concurrency)
                .Build();
        }

        public SendPayload ToSendPayload(Listen listen)
        {
            var sendPayload = _bufferPool.RendSendPayload();

            return sendPayload
                .WriteType(PayloadType.Listen)
                .WriteId(listen.Id)
                .WriteStr(listen.Route)
                .Build();
        }

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

                data.Slice(18 + indexOfRouteDelimiter).CopyTo(messageMemoryOwner.AsSpan());

                return new Message
                {

                    Id = messageId,
                    Route = route,
                    Data = messageMemoryOwner,
                    OriginalMessageData = messageMemoryOwner
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
    }
}
