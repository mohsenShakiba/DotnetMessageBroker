using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Common
{
    public class InMemoryMessageQueue<T> : IQueue<T>
    {

        private readonly ConcurrentStack<T> _messageQueue = new();

        public void Push(T item)
        {
            _messageQueue.Push(item);
        }

        public bool TryPop(out T item)
        {
            var result = _messageQueue.TryPop(out item);
            return result;
        }
    }
}
