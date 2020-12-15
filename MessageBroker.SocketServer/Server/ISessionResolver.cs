using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public interface ISessionResolver
    {
        void AddSession(ClientSession session);
        void RemoveSession(Guid sessionId);

        ClientSession ResolveSession(Guid guid);
    }
}
