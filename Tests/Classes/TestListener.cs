using System;
using MessageBroker.Common.Tcp;
using MessageBroker.Common.Tcp.EventArgs;

namespace Tests.Classes
{
    public class TestListener : IListener
    {
        public event EventHandler<SocketAcceptedEventArgs> OnSocketAccepted;

        public void Start()
        {
            // no-op
        }

        public void Stop()
        {
            // no-op
        }

        public void Dispose()
        {
            // no-op
        }

        public void AcceptTestSocket(ISocket socket)
        {
            OnSocketAccepted?.Invoke(this, new SocketAcceptedEventArgs {Socket = socket});
        }

        public TestSocket CreateTestSocket()
        {
            return new();
        }
    }
}