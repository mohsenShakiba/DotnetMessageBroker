using System;
using MessageBroker.TCP.Client;

namespace MessageBroker.TCP
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