using System;
using MessageBroker.Socket.Client;

namespace MessageBroker.Socket
{
    public interface ISocketEventProcessor
    {
        void ClientDisconnected(IClientSession clientSession);
        void ClientConnected(IClientSession clientSession);
    }

    public interface ISocketDataProcessor
    {
        void DataReceived(Guid sessionId, Memory<byte> data);
    }
}