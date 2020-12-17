using System;

namespace MessageBroker.Core.MessageProcessor
{
    public interface IMessageProcessor
    {
        void ClientConnected(Guid sessionId);
        void ClientDisconnected(Guid sessionId);
        void DataReceived(Guid sessionId, Memory<byte> data);
    }
}
