using System;
using MessageBroker.Client.SocketClient;
using MessageBroker.Core.Socket;
using MessageBroker.Core.Socket.Client;

namespace Tests.Classes
{
    public class TestSocketEventProcessor : ISocketEventProcessor
    {
        public void ClientConnected(IClientSession clientSession)
        {
            OnClientConnected?.Invoke(clientSession.Id);
        }

        public void ClientDisconnected(IClientSession clientSession)
        {
            OnClientDisconnected?.Invoke(clientSession.Id);
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