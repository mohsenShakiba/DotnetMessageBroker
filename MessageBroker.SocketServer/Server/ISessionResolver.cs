using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public interface ISessionResolver
    {
        void AddSession(IClientSession session);
        void RemoveSession(Guid sessionId);

        IClientSession ResolveSession(Guid guid);
        IReadOnlyList<IClientSession> Sessions { get; }
    }
}
