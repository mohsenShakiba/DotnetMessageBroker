using System;
using MessageBroker.Core.Socket.Client;

namespace MessageBroker.Core.Socket
{
    public interface ISocketEventProcessor
    {
        void ClientConnected(IClientSession clientSession);
        void ClientDisconnected(IClientSession clientSession);
        void DataReceived(Guid sessionId, Memory<byte> data);
    }
}