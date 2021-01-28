using System;
using System.Collections.Concurrent;

namespace MessageBroker.Core.MessageIdTracking
{
    public class MessageIdTracker: IMessageIdTracker
    {
        private readonly ConcurrentDictionary<Guid, string> _messageQueueMapper;

        public MessageIdTracker()
        {
            _messageQueueMapper = new();
        }

        public void BindMessageIdToQueue(Guid id, string queueName)
        {
            _messageQueueMapper.TryAdd(id, queueName);
        }

        public string ResolveMessageId(Guid id)
        {
            if (_messageQueueMapper.TryRemove(id, out var queueName))
                return queueName;

            return null;
        }
        
    }
}