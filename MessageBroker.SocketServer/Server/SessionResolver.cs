using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public class SessionResolver : ISessionResolver
    {

        private readonly ConcurrentDictionary<Guid, ClientSession> _sesions;

        public SessionResolver()
        {
            _sesions = new();
        }

        public void AddSession(ClientSession session)
        {
            _sesions[session.SessionId] = session;
        }

        public void RemoveSession(Guid sessionId)
        {
            _sesions.TryRemove(sessionId, out _);
        }

        public ClientSession ResolveSession(Guid guid)
        {
            return _sesions[guid];
        }
    }
}
