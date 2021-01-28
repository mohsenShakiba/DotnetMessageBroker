using System;
using MessageBroker.Models;

namespace MessageBroker.Core.Queues
{
    public interface IQueue
    {
        string Name { get; }
        string Route { get; }
        void Setup(string name, string route);
        void OnMessage(Message message);
        void OnMessageAck(Guid sessionId, Guid messageId);
        void OnMessageNack(Guid sessionId, Guid messageId);
        void SessionSubscribed(Guid sessionId);
        void SessionUnSubscribed(Guid sessionId);
        void SessionDisconnected(Guid sessionId);
        bool MessageRouteMatch(string messageRoute);
    }
}