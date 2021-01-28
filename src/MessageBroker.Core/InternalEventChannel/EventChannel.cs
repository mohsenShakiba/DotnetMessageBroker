using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Core.InternalEventChannel
{
    public class EventChannel: IEventChannel
    {

        private readonly ConcurrentDictionary<string, Channel<InternalEvent>> _channels;
        private readonly ConcurrentDictionary<Guid, Channel<InternalEvent>> _messageChannelDict;

        public EventChannel()
        {
            _channels = new();
            _messageChannelDict = new();
        }
        
        public Channel<InternalEvent> GetListenChannelForQueueName(string queueName)
        {
            if (_channels.TryGetValue(queueName, out var chan))
            {
                return chan;
            }

            chan = Channel.CreateUnbounded<InternalEvent>();
            
            _channels[queueName] = chan;

            return chan;
        }

        public void ListenToEventForId(string queueName, Guid id)
        {
            if (_channels.TryGetValue(queueName, out var chan))
            {
                _messageChannelDict[id] = chan;
            }
        }

        public void OnMessageSent(Guid sessionId, Guid messageId, bool autoAck)
        {
            if (_messageChannelDict.TryRemove(messageId, out var chan))
            {
                var ev = ObjectPool.Shared.Rent<InternalEvent>();
                
                ev.Ack = true;
                ev.MessageId = messageId;
                ev.SessionId = sessionId;
                ev.AutoAck = autoAck;
                
                chan.Writer.TryWrite(ev);
            }
        }

        public void OnMessageError(Guid sessionId, Guid messageId)
        {
            if (_messageChannelDict.TryRemove(messageId, out var chan))
            {
                var ev = ObjectPool.Shared.Rent<InternalEvent>();

                ev.Ack = false;
                ev.MessageId = messageId;
                ev.SessionId = sessionId;
                ev.AutoAck = false;
                
                chan.Writer.TryWrite(ev);
            }
        }
    }
}