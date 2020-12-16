using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Common
{
    public class MessageProcessor : IMessageProcessor
    {
        public event Action<Guid, Memory<byte>> OnMessageReceived;
        public event Action<Guid> OnClientConnected;
        public event Action<Guid> OnClientDisconnected;

        public void ClientConnected(Guid sessionId)
        {
            OnClientConnected?.Invoke(sessionId);
        }

        public void ClientDisconnected(Guid sessionId)
        {
            OnClientDisconnected?.Invoke(sessionId);
        }

        public void MessageReceived(Guid sessionId, Memory<byte> payload)
        {
            OnMessageReceived?.Invoke(sessionId, payload);
        }

    }
}
