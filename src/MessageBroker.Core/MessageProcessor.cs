using MessageBroker.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core
{
    public class MessageProcessor : IMessageProcessor
    {

        private readonly Coordinator _cordinator = new();

        public void OnClientConnected(Guid sessionId)
        {
        }

        public void OnClientDisconnected(Guid sessionId)
        {
            _cordinator.OnClientDisconnected(sessionId);
        }

        public void OnMessage(Payload payload)
        {
            
        }

        private void ProcessMessage()
        {

        }
    }
}
