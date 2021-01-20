using System;
using MessageBroker.SocketServer.Abstractions;

namespace Tests.Classes
{
    public class TestSocketEventProcessor : ISocketEventProcessor
    {
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

        public event Action<Guid, Memory<byte>> OnDataReceived;
        public event Action<Guid> OnClientConnected;
        public event Action<Guid> OnClientDisconnected;
    }
}