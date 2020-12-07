using MessageBroker.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Core
{
    class Subscriber
    {
        private readonly IList<string> _listenRoutes;

        public Guid SessionId { get; private set; }
        public bool Connected { get; private set; }

        public Subscriber(Guid sessionId)
        {
            SessionId = sessionId;
            Connected = true;
            _listenRoutes = new List<string>();
        }

        public void AddRoute(string route)
        {
            _listenRoutes.Add(route);
        }

        public void RemoveRoute(string route)
        {
            _listenRoutes.Remove(route);
        }

        public void SetAsDisconnected()
        {
            Connected = false;
        }

        public bool MatchRoute(string route, IRouteMatcher routeMatcher)
        {
            foreach(var listenRoute in _listenRoutes)
            {
                if (routeMatcher.Match(route, listenRoute))
                    return true;
            }
            return false;
        }
    }
}
