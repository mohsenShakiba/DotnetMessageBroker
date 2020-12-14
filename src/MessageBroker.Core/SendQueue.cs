using MessageBroker.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core
{
    public class SendQueue
    {
        private ConcurrentQueue<Message> _queue;
        private 

        public SendQueue()
        {
            _queue = new();
        }


    }
}
