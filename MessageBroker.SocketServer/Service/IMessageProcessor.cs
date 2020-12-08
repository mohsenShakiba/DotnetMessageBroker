using MessageBroker.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core
{
    public interface IMessageProcessor
    {
        void OnClientConnected(Guid sessionId);
        void OnClientDisconnected(Guid sessionId);
        void OnMessage(Payload payload);
    }
}
