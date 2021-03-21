using System;
using System.Threading.Tasks;
using MessageBroker.Models;

namespace MessageBroker.Core.Queues
{
    public interface IQueue
    {
        string Name { get; }
        string Route { get; }
        void Setup(string name, string route);
        Task ReadNextMessage();
        void OnMessage(Message message);
        void SessionSubscribed(Guid sessionId);
        void SessionUnSubscribed(Guid sessionId);
        void SessionDisconnected(Guid sessionId);
        bool MessageRouteMatch(string messageRoute);
    }
}