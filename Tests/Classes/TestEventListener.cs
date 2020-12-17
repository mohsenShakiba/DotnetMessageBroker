using MessageBroker.SocketServer.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Classes
{
    public class TestEventListener : ISessionEventListener
    {

        public event Action<Guid, Memory<byte>> ReceivedEvent;
        public event Action<Guid> SessionDisconnectedEvent;
        public void OnReceived(Guid sessionId, Memory<byte> data)
        {
            ReceivedEvent?.Invoke(sessionId, data);
        }

        public void OnSessionDisconnected(Guid sessionId)
        {
            SessionDisconnectedEvent?.Invoke(sessionId);
        }
    }
}
