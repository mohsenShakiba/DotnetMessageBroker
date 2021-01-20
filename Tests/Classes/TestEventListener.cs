using System;
using MessageBroker.SocketServer.Abstractions;

namespace Tests.Classes
{
    public class TestEventListener : ISessionEventListener
    {
        public void OnReceived(Guid sessionId, Memory<byte> data)
        {
            ReceivedEvent?.Invoke(sessionId, data);
        }

        public void OnSessionDisconnected(Guid sessionId)
        {
            SessionDisconnectedEvent?.Invoke(sessionId);
        }

        public event Action<Guid, Memory<byte>> ReceivedEvent;
        public event Action<Guid> SessionDisconnectedEvent;
    }
}