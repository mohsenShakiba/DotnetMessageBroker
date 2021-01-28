using System;
using System.Threading.Channels;

namespace MessageBroker.Core.InternalEventChannel
{
    public interface IEventChannel
    {
        Channel<InternalEvent> GetListenChannelForQueueName(string queueName);
        void ListenToEventForId(string queueName, Guid id);
        void OnMessageSent(Guid sessionId, Guid messageId, bool autoAck);
        void OnMessageError(Guid sessionId, Guid messageId);
    }
}