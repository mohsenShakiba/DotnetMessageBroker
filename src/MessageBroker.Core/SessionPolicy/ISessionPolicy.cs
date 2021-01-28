using System;

namespace MessageBroker.Core.SessionPolicy
{
    public interface ISessionPolicy
    {
        void AddSession(Guid sessionId);
        void RemoveSession(Guid sessionId);
        Guid? GetNextSession();
    }
}