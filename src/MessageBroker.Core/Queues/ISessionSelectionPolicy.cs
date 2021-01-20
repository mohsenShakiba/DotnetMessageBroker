using System;

namespace MessageBroker.Core.Queues
{
    public interface ISessionSelectionPolicy
    {
        void AddSession(Guid sessionId);
        void RemoveSession(Guid sessionId);
        Guid? GetNextSession();
    }
}