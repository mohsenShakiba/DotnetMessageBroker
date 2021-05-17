using System;
using MessageBroker.TCP;
using MessageBroker.TCP.EventArgs;

namespace Tests.Classes
{
    public class TestListener: IListener
    {
        public event EventHandler<SocketAcceptedEventArgs> OnSocketAccepted;

        public void AcceptTestSocket(ISocket socket)
        {
            OnSocketAccepted?.Invoke(this, new SocketAcceptedEventArgs {Socket = socket});
        }

        public TestSocket CreateTestSocket()
        {
            return new TestSocket();
        }

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
    }
}