using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    interface ISessionResolver
    {
        void AddSession(ClientSession session);
        void RemoveSession(ClientSession session);

        ClientSession ResolveSession(Guid guid);
    }
}
