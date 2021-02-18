using System;
using System.Buffers;
using System.Threading.Tasks;
using MessageBroker.Socket.SocketWrapper;
using Xunit.Sdk;

namespace Tests.Classes
{
    public class TestTcpSocket: ITcpSocket
    {

        private Memory<byte> _memory;
        private int _offset;
        private object _lock;
        
        public TestTcpSocket()
        {
            _memory = ArrayPool<byte>.Shared.Rent(1024);
            _lock = new();
        }
        
        public void Close()
        {
            // nothing
        }

        public ValueTask<int> SendAsync(Memory<byte> data)
        {
            lock (_lock)
            {
                data.CopyTo(_memory.Slice(_offset));
                _offset += data.Length;
            }

            return ValueTask.FromResult(data.Length);
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer)
        {
            while (_offset == 0)
                Task.Delay(100);
            
            var size = 0;
            
            lock (_lock)
            {
                _memory.TryCopyTo(buffer);
                size = _offset;

                _offset = 0;
            }

            return ValueTask.FromResult(size);
        }
    }
}