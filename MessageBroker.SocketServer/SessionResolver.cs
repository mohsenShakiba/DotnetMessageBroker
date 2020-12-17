using MessageBroker.SocketServer.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MessageBroker.SocketServer
{
    public class SessionResolver : ISessionResolver
    {

        private readonly ConcurrentDictionary<Guid, IClientSession> _sesions;

        public IReadOnlyList<IClientSession> Sessions => _sesions.Values.ToList();


        public SessionResolver()
        {
            _sesions = new();
        }

        public void Add(IClientSession session)
        {
            _sesions[session.SessionId] = session;
        }

        public void Remove(Guid sessionId)
        {
            _sesions.TryRemove(sessionId, out _);
        }

        public IClientSession Resolve(Guid guid)
        {
            _sesions.TryGetValue(guid, out var session);
            return session;
        }
    }
}
