using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Queue
{
    public interface IQueue
    {
        void SessionSubscribed(string route, Guid sessionId);
        void SessionUnSubscribed(string route, Guid sessionId);
        void SessionDisconnected(Guid sessionId);
    }
}
