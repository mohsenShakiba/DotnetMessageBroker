using System;

namespace MessageBroker.Common
{
    public interface IMessageProcessor
    {

        event Action<(Guid, Memory<byte>)> OnMessageReceived;
        event Action<Guid> OnClientConnected;
        event Action<Guid> OnClientDisconnected;

        void ClientConnected(Guid sessionId);
        void ClientDisconnected(Guid sessionId);
        void MessageReceived(Guid sessionId, Memory<byte> payload);
    }
}
