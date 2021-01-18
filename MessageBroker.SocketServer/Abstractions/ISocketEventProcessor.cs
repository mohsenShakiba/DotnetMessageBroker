using System;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface ISocketEventProcessor
    {
        void ClientConnected(Guid sessionId);
        void ClientDisconnected(Guid sessionId);
        void DataReceived(Guid sessionId, Memory<byte> data);
    }
}
