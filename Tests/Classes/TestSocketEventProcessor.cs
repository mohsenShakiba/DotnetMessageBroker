using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageBroker.SocketServer.Abstractions;

namespace Tests.Classes
{
    public class TestSocketEventProcessor : ISocketEventProcessor
    {

        public event Action<Guid, Memory<byte>> OnDataReceived;
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

        public void DataReceived(Guid sessionId, Memory<byte> payload)
        {
            OnDataReceived?.Invoke(sessionId, payload);
        }
    }
}
