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

        private readonly ConcurrentDictionary<Guid, IClientSession> _sesions;
        public IReadOnlyList<IClientSession> Sessions => _sesions.Values.ToList();



        public SessionResolver()
        {
            _sesions = new();
        }


        public void AddSession(IClientSession session)
        {
            _sesions[session.SessionId] = session;
        }

        public void RemoveSession(Guid sessionId)
        {
            _sesions.TryRemove(sessionId, out _);
        }

        public IClientSession ResolveSession(Guid guid)
        {
            return _sesions[guid];
        }
    }
}
