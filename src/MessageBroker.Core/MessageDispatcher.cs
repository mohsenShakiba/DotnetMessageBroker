using MessageBroker.Messages;
using MessageBroker.SocketServer.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core
{
    public class MessageDispatcher
    {
        private readonly ISessionResolver _sessionResolver;
        private readonly ConcurrentDictionary<Guid, SendQueue> _sendQueues;


        public MessageDispatcher(ISessionResolver sessionResolver)
        {
            _sessionResolver = sessionResolver;
            _sendQueues = new();
        }

        public SendQueue GetSendQueue(Guid sessionId)
        {
            if (_sendQueues.TryGetValue(sessionId, out var sendQueue))
            {
                return sendQueue;
            }

            return null;
        }

        public void Dispatch(Message msg, IEnumerable<Guid> destinations)
        {
            foreach(var destination in destinations)
            {
                if (_sendQueues.TryGetValue(destination, out var sendQueue))
                {
                    sendQueue.Enqueue(msg);
                }
                else
                {
                    var session = _sessionResolver.ResolveSession(destination);
                    if (session != null)
                    {
                        var queue = new SendQueue(session);
                        queue.Enqueue(msg);
                        _sendQueues[destination] = queue;
                    }
                }
            }
        }

        public void Release(Ack ack, Guid[] destinations)
        {
            foreach(var destination in destinations)
            {
                if (_sendQueues.TryGetValue(destination, out var sendQueue))
                {
                    sendQueue.ReleaseOne(ack);
                }
            }
        }

    }
}
