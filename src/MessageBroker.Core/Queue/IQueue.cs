using MessageBroker.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Queue
{
    public interface IQueue
    {
        void Setup(string name, string route);
        void OnMessage(Message message);
        void OnAck(Ack ack);
        void OnNack(Ack nack);
        void SessionSubscribed(string route, Guid sessionId);
        void SessionUnSubscribed(string route, Guid sessionId);
        void SessionDisconnected(Guid sessionId);
        bool MessageRouteMatch(string messageRoute);
    }
}
