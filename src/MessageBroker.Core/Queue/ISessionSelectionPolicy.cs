using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Queue
{
    public interface ISessionSelectionPolicy
    {
        void AddSession(Guid sessionId);
        void RemoveSession(Guid sessionId);
        Guid? GetNextSession(string route);
    }
}
