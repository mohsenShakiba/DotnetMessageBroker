using System;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface IClientSession
    {
        Guid SessionId { get; }
        void SetupSendCompletedHandler(Action onSendCompleted);
        void Send(Memory<byte> payload);
        bool SendAsync(Memory<byte> payload);
        void Close();
        void Dispose();
    }
}