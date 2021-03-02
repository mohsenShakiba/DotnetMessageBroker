using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;
using MessageBroker.TCP.SocketWrapper;
using Xunit.Sdk;

namespace Tests.Classes
{
    public class TestTcpSocket: ITcpSocket
    {

        private Memory<byte> _memory;
        private int _offset;
        private object _lock;

        private bool _connected;
        public bool AllowConnect { get; set; } = true;
        
        public TestTcpSocket()
        {
            _memory = ArrayPool<byte>.Shared.Rent(1024);
            _lock = new();
        }

        public bool Connected => _connected;

        public void Close()
        {
            // nothing
        }

        public void Connect(IPEndPoint ipEndPoint)
        {
            if (!AllowConnect)
                throw new Exception("Socket cannot be connected at this time");
            
            _connected = true;
        }

        public void Reconnect(IPEndPoint ipEndPoint)
        {
            Connect(ipEndPoint);
        }

        public void Disconnect(bool reuseSocket)
        {
            _connected = false;
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