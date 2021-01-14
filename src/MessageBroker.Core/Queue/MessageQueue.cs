using MessageBroker.Core.Models;
using MessageBroker.Core.Persistance;
using MessageBroker.Core.RouteMatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Queue
{
    class MessageQueue : IQueue, IDisposable
    {
        private readonly MessageDispatcher _dispatcher;
        private readonly ISessionSelectionPolicy _sessionSelectionPolicy;
        private readonly IMessageStore _messageStore;
        private readonly IRouteMatcher _routeMatcher;

        private string _name;
        private string _route;

        public MessageQueue(MessageDispatcher dispatcher, ISessionSelectionPolicy sessionSelectionPolicy, IMessageStore messageStore, 
            IRouteMatcher routeMatcher)
        {
            _dispatcher = dispatcher;
            _sessionSelectionPolicy = sessionSelectionPolicy;
            _messageStore = messageStore;
            _routeMatcher = routeMatcher;
        }

        public void Setup(string name, string route)
        {
            _name = name;
            _route = route;
        }

        public void OnMessage(Message message)
        {
            // todo: persist the message 

            var sessionId = _sessionSelectionPolicy.GetNextSession();

            if (sessionId.HasValue)
            {
                _dispatcher.Dispatch(message, sessionId.Value);
            }
        }

        public bool MessageRouteMatch(string messageRoute)
        {
            return _routeMatcher.Match(messageRoute, _route);
        }

        public void SessionDisconnected(Guid sessionId)
        {
            _sessionSelectionPolicy.RemoveSession(sessionId);
        }

        public void SessionSubscribed(Guid sessionId)
        {
            _sessionSelectionPolicy.AddSession(sessionId);
        }

        public void SessionUnSubscribed(Guid sessionId)
        {
            SessionDisconnected(sessionId);
        }
        public void Dispose()
        {
            // nothing for now
        }

        public void OnAck(Ack ack)
        {
            _messageStore.DeleteAsync(ack.Id);
        }

        public void OnNack(Ack nack)
        {
            // todo: find the message 
            // call on message again
        }
    }
}
