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

        public byte[] Serialize(object o)
        {
            return o switch
            {
                Ack ack => ToBinary(ack),
                Nack nack => ToBinary(nack),
                Message message => ToBinary(message),
                Listen listen => ToBinary(listen),
                Unlisten unlisten => ToBinary(unlisten),
                Subscribe subscribe => ToBinary(subscribe),
                _ => throw new NotImplementedException(),
            };
        }

        private byte[] ToBinary(Ack ack)
        {
            var buff = new List<byte>();

            buff.AddWithDelimiter(ModelTypes.Ack);
            buff.AddWithDelimiter(ack.Id);

            ack.Id.TryWriteBytes()

            return buff.ToArray();
        }

        private byte[] ToBinary(Nack nack)
        {
            var buff = new List<byte>();

            buff.AddWithDelimiter(ModelTypes.Nack);
            buff.AddWithDelimiter(nack.Id);

            return buff.ToArray();
        }

        private byte[] ToBinary(Message msg)
        {
            var buff = new List<byte>();

            buff.AddWithDelimiter(ModelTypes.Message);
            buff.AddWithDelimiter(msg.Id);
            buff.AddWithDelimiter(msg.Route);
            buff.AddWithDelimiter(msg.Data.ToArray());

            return buff.ToArray();
        }

        private byte[] ToBinary(Listen listen)
        {
            var buff = new List<byte>();

            buff.AddWithDelimiter(ModelTypes.Listen);
            buff.AddWithDelimiter(listen.Id);
            buff.AddWithDelimiter(listen.Route);

            return buff.ToArray();
        }

        private byte[] ToBinary(Unlisten unlisten)
        {
            var buff = new List<byte>();

            buff.AddWithDelimiter(ModelTypes.Unlisten);
            buff.AddWithDelimiter(unlisten.Id);
            buff.AddWithDelimiter(unlisten.Route);

            return buff.ToArray();
        }

        private byte[] ToBinary(Subscribe subscribe)
        {
            var buff = new List<byte>();

            buff.AddWithDelimiter(ModelTypes.Subscribe);
            buff.AddWithDelimiter(subscribe.Id);
            buff.AddWithDelimiter(subscribe.Concurrency);

            return buff.ToArray();
        }

        #endregion

        #region Deserialize

        public object Deserialize(Memory<byte> b)
        {
            var payload = b.Span;
            var indexOfMessageType = payload.IndexOf(Delimiter);
            var messasgeType = payload.Slice(0, indexOfMessageType);
            var messageBody = payload.Slice(indexOfMessageType + Delimiter.Length);

            var messageTypeStr = Encoding.ASCII.GetString(messasgeType);

            return messageTypeStr switch
            {
                ModelTypes.Ack => ToAck(messageBody),
                ModelTypes.Nack => ToNack(messageBody),
                ModelTypes.Message => ToMessage(messageBody),
                ModelTypes.Listen => ToListenRoute(messageBody),
                ModelTypes.Unlisten => ToUnlistenRoute(messageBody),
                ModelTypes.Subscribe => ToSubscribe(messageBody),
                _ => null
            };
        }


        private Ack ToAck(Span<byte> data)
        {
            var dataTrimmed = data.TrimEnd(Delimiter);
            var messageId = new Guid(dataTrimmed);
            return new Ack(messageId);
        }

        private Nack ToNack(Span<byte> data)
        {
            var dataTrimmed = data.TrimEnd(Delimiter);
            var messageId = new Guid(dataTrimmed);
            return new Nack(messageId);
        }

        private Message ToMessage(Span<byte> data)
        {
            var messageId = new Guid(data.Slice(0, 16));
            var indexOfRouteDelimiter = data.Slice(17).IndexOf(Delimiter);
            var route = Encoding.UTF8.GetString(data.Slice(17, indexOfRouteDelimiter));
            var payload = data.Slice(17 + indexOfRouteDelimiter + 1, data.Length  - (17 + indexOfRouteDelimiter) - 2);
            var rentedMemory = ArrayPool<byte>.Shared.Rent(payload.Length);
            payload.CopyTo(rentedMemory);

            return new Message(messageId, route, rentedMemory);
        }

        private Listen ToListenRoute(Span<byte> data)
        {
            var id = new Guid(data.Slice(0, 16));
            var route = Encoding.UTF8.GetString(data.Slice(17, data.Length - 18));

            return new Listen(id, route);
        }

        private Unlisten ToUnlistenRoute(Span<byte> data)
        {
            var id = new Guid(data.Slice(0, 16));
            var route = Encoding.UTF8.GetString(data.Slice(17, data.Length - 18));

            return new Unlisten(id, route);
        }

        private Subscribe ToSubscribe(Span<byte> data)
        {
            var id = new Guid(data.Slice(0, 16));
            var concurrency = BitConverter.ToInt32(data.Slice(17, 4));

            return new Subscribe(id, concurrency);
        }

        #endregion

    }
}
