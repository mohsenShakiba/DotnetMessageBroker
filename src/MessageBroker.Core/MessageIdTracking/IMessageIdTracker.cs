using System;

namespace MessageBroker.Core.MessageIdTracking
{
    public interface IMessageIdTracker
    {
        void BindMessageIdToQueue(Guid id, string queueName);
        string ResolveMessageId(Guid id);
    }
}