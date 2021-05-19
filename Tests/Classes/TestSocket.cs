using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Tcp;

namespace Tests.Classes
{
    public class TestSocket : ISocket
    {
        private Memory<byte> _data;
        public bool Connected { get; private set; } = true;

        public void Disconnect()
        {
            Connected = false;
        }

        public void SimulateInterrupt()
        {
            // no-op
        }

        public ValueTask<int> SendAsync(Memory<byte> data, CancellationToken cancellationToken)
        {
            if (!Connected)
                return ValueTask.FromResult(0);

            return ValueTask.FromResult(data.Length);
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!Connected)
                    return 0;

                await Task.Delay(100, cancellationToken);

                if (_data.Length > 0)
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

        public void Dispose()
        {
            Disconnect();
        }

        public void SendTestData(Memory<byte> data)
        {
            _data = data;
        }
    }
}