using System;
using MessageBroker.Models.Models;

namespace MessageBroker.Core.Queues
{
    public interface IQueue
    {
        void Setup(string name, string route);
        void OnMessage(Message message);
        void OnAck(Ack ack);
        void OnNack(Ack nack);
        void SessionSubscribed(Guid sessionId);
        void SessionUnSubscribed(Guid sessionId);
        void SessionDisconnected(Guid sessionId);
        bool MessageRouteMatch(string messageRoute);
    }
}