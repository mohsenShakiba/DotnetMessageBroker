using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Queue
{
    class MesssageQueue : IQueue
    {
        private string _route { get; set; }
        private readonly ISessionSelectionPolicy _sessionSelectionPolicy;

        public void SessionDisconnected(Guid sessionId)
        {
            _sessionSelectionPolicy.RemoveSession(sessionId);
        }

        public void SessionSubscribed(string route, Guid sessionId)
        {
            _sessionSelectionPolicy.AddSession(sessionId);
        }

        public void SessionUnSubscribed(string route, Guid sessionId)
        {
            SessionDisconnected(sessionId);
        }


    }
}
