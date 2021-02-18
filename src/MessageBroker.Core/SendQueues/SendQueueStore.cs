using System;
using System.Collections.Concurrent;
using MessageBroker.Socket.Client;

namespace MessageBroker.Core
{
    public class SendQueueStore: ISendQueueStore
    {
        private readonly ConcurrentDictionary<Guid, ISendQueue> _sendQueues;


        public SendQueueStore()
        {
            _sendQueues = new ConcurrentDictionary<Guid, ISendQueue>();
        }
        

        public void Add(IClientSession clientSession)
        {
            var sendQueue = new SendQueue(clientSession);
            _sendQueues[clientSession.Id] = sendQueue;
        }

        public void Remove(IClientSession clientSession)
        {
            if (_sendQueues.TryRemove(clientSession.Id, out var sendQueue))
            {
                sendQueue.Stop();
                clientSession.Close();
            }
        }

        public bool TryGet(Guid sessionId, out ISendQueue sendQueue)
        {
            return _sendQueues.TryGetValue(sessionId, out sendQueue);
        }
    }
}