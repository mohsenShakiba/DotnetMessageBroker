using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.TCP;

namespace Tests.Classes
{
    public class TestSocket: ISocket
    {
        private bool _connected = true;
        private Memory<byte> _data = default;
        public bool Connected => _connected;
        
        public void SendTestData(Memory<byte> data)
        {
            _data = data;
        }

        public void Disconnect()
        {
            _connected = false;
        }

        public void SimulateInterrupt()
        {
            // no-op
        }

        public ValueTask<int> SendAsync(Memory<byte> data, CancellationToken cancellationToken)
        {
            if (!_connected)
                return ValueTask.FromResult(0);

            return ValueTask.FromResult(data.Length);
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!_connected)
                    return 0;
                
                await Task.Delay(100, cancellationToken);

                if (_data.Length > 0)
                {
                    try
                    {
                        _data.CopyTo(buffer);
                        return _data.Length;
                    }
                    finally
                    {
                        _data = default;
                    }
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}