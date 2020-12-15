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
        private readonly SessionResolver _sessionResolver;
        private readonly ConcurrentDictionary<Guid, SendQueue> _sendQueues;


        public MessageDispatcher(SessionResolver sessionResolver)
        {
            _sessionResolver = sessionResolver;
        }

        public void Dispatch(MessageDestination msgDestination)
        {
            foreach(var destination in msgDestination.Destinations)
            {
                if (_sendQueues.TryGetValue(destination, out var sendQueue))
                {
                    sendQueue.Enqueue(msgDestination.Data);
                }
                else
                {
                    var session = _sessionResolver.ResolveSession(destination);
                    if (session != null)
                    {
                        var queue = new SendQueue(session);
                        queue.Enqueue(msgDestination.Data);
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

    public class MessageDestination
    {
        public IList<Guid> Destinations { get; set; }
        public Message Data { get; set; }
    }
}
