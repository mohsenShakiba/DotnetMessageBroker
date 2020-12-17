using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface ISessionResolver
    {
        void Add(IClientSession session);
        void Remove(Guid sessionId);
        IClientSession Resolve(Guid guid);
        IReadOnlyList<IClientSession> Sessions { get; }
    }
}
