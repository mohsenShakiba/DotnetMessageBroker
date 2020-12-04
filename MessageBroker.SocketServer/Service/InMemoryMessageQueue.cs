using MessageBroker.SocketServer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Service
{
    public class InMemoryMessageQueue : IMessageQueue
    {

        private readonly ConcurrentStack<MessagePayload> _messageQueue = new();

        public ValueTask Push(MessagePayload msg)
        {
            _messageQueue.Push(msg);
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryPop(out MessagePayload msg)
        {
            var result = _messageQueue.TryPop(out msg);
            return ValueTask.FromResult(result);
        }
    }
}
