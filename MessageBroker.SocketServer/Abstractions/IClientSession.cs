using System;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface IClientSession
    {
        Guid SessionId { get; }
        void SetupSendCompletedHandler(Action<Guid> onSendCompleted, Action<Guid> onMessageError);
        void SetSendPayloadId(Guid sendPayloadId);
        void Send(Memory<byte> payload);
        bool SendAsync(Memory<byte> payload);
        void Close();
        void Dispose();
    }
}