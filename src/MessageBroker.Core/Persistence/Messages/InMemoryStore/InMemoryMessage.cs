using System;
using System.Buffers;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence.Messages.InMemoryStore
{
    public class InMemoryMessage
    {
        private byte[] _buffer;

        private int _size;
        public Guid Id { get; private set; }
        public string Route { get; private set; }
        public string QueueName { get; private set; }

        public Memory<byte> Data => _buffer.AsMemory(0, _size);


        public void FillFrom(QueueMessage message, bool useDataInMessage = false)
        {
            var messageSize = message.Data.Length;
            if (useDataInMessage)
            {
                if (_buffer != null)
                    ArrayPool<byte>.Shared.Return(_buffer);

                _buffer = message.OriginalMessageData;
            }
            else
            {
                if ((_buffer?.Length ?? 0) < messageSize)
                {
                    if (_buffer != null)
                        ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = ArrayPool<byte>.Shared.Rent(messageSize);
                }

                message.Data.CopyTo(_buffer.AsMemory());
            }


            Id = message.Id;
            Route = message.Route;
            QueueName = message.QueueName;
            _size = messageSize;
        }
    }
}