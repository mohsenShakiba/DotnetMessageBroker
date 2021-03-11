using System;
using System.Collections.Concurrent;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    public class SendQueueStore: ISendQueueStore
    {
        private readonly ConcurrentDictionary<Guid, ISendQueue> _sendQueues;


        public SendQueueStore()
        {
            _sendQueues = new ConcurrentDictionary<Guid, ISendQueue>();
        }
        

        public ISendQueue Add(IClientSession clientSession, ISendQueue sendQueue = null)
        {
            sendQueue ??= new SendQueue(clientSession);
            _sendQueues[clientSession.Id] = sendQueue;
            return sendQueue;
        }

        public ISendQueue Remove(IClientSession clientSession)
        {
            if (_sendQueues.TryRemove(clientSession.Id, out var sendQueue))
            {
                sendQueue.Stop();
                return sendQueue;
            }

            return null;
        }

        public bool TryGet(Guid sessionId, out ISendQueue sendQueue)
        {
            return _sendQueues.TryGetValue(sessionId, out sendQueue);
        }
    }
}