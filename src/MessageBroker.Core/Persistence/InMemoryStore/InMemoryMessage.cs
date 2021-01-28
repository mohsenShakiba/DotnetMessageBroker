using System;
using System.Buffers;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence.InMemoryStore
{
    public class InMemoryMessage
    {
        public Guid Id { get; private set; }
        public string Route { get; private set; }
        
        public Memory<byte> Data => _buffer.AsMemory(0, _size);
        
        private int _size;
        private byte[] _buffer;


        public void FillFrom(Message message)
        {
            var messageSize = message.Data.Length;
            
            if ((_buffer?.Length ?? 0) < messageSize)
            {
                if (_buffer != null)
                    ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(messageSize);
            }

            message.Data.CopyTo(_buffer.AsMemory());

            Id = message.Id;
            Route = message.Route;
            _size = messageSize;
        }
    
    }
}