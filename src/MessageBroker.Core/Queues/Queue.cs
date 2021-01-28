using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.InternalEventChannel;
using MessageBroker.Core.MessageIdTracking;
using MessageBroker.Core.Persistence;
using MessageBroker.Core.Persistence.InMemoryStore;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.SessionPolicy;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Core.Queues
{
    public class Queue : IQueue, IDisposable
    {
        private readonly MessageDispatcher _dispatcher;
        private readonly IMessageStore _messageStore;
        private readonly IRouteMatcher _routeMatcher;
        private readonly IEventChannel _eventChannel;
        private readonly ISerializer _serializer;
        private readonly IMessageIdTracker _messageIdTracker;
        private readonly ISessionPolicy _sessionPolicy;
        private readonly Channel<Guid> _queue;

        private Channel<InternalEvent> _internalEventChan;
        private string _name;
        private string _route;
        private bool _stopped;

        public string Name => _name;
        public string Route => _route;

        public Queue(MessageDispatcher dispatcher, ISessionPolicy sessionPolicy,
            IMessageStore messageStore, IRouteMatcher routeMatcher, IEventChannel eventChannel, ISerializer serializer, IMessageIdTracker messageIdTracker)
        {
            _dispatcher = dispatcher;
            _sessionPolicy = sessionPolicy;
            _messageStore = messageStore;
            _routeMatcher = routeMatcher;
            _eventChannel = eventChannel;
            _serializer = serializer;
            _messageIdTracker = messageIdTracker;
            _queue = Channel.CreateUnbounded<Guid>();
        }

        public void Dispose()
        {
            _stopped = true;
        }

        public void Setup(string name, string route)
        {
            _name = name;
            _route = route;
            _internalEventChan = _eventChannel.GetListenChannelForQueueName(_name);

            SetupSendQueueProcessor();
            SetupInternalEventQueueProcessor();
        }

        private void SetupInternalEventQueueProcessor()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (_stopped)
                        return;

                    var ev = await _internalEventChan.Reader.ReadAsync();

                    if (ev.Ack)
                        OnMessageSentSuccess(ev.SessionId, ev.MessageId, ev.AutoAck);
                    else
                        OnMessageSentError(ev.SessionId, ev.MessageId);
                    
                    ObjectPool.Shared.Return(ev);
                }
            });
        }

        private void SetupSendQueueProcessor()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (_stopped)
                        return;
                    
                    
                    
                    var messageIds = _queue.Reader.ReadAllAsync();
                    
                    await foreach (var messageId in messageIds)
                    {
                        ProcessMessage(messageId);
                    }
                }
            });
        }

        private void ProcessMessage(Guid messageId)
        {
            if (_messageStore.TryGetValue(messageId, out var message))
            {
                var sendPayload = _serializer.ToSendPayload(message);
                SendMessage(sendPayload);
            }
        }

        public void OnMessage(Message message)
        {
            // set new id for this message
            // each message will be assigned a new id
            message.SetNewId();
            
            // track id with queue
            _messageIdTracker.BindMessageIdToQueue(message.Id, _name);
            
            // persist the message
            _messageStore.InsertAsync(message);
            
            // add the message to queue chan
            _queue.Writer.TryWrite(message.Id);
        }

        private void SendMessage(SendPayload sendPayload)
        {
            var sessionId = _sessionPolicy.GetNextSession();

            if (sessionId.HasValue)
            {
                if (sendPayload.IsMessageType)
                    _eventChannel.ListenToEventForId(_name, sendPayload.Id);
                _dispatcher.Dispatch(sendPayload, sessionId.Value);
            };
        }

        public bool MessageRouteMatch(string messageRoute)
        {
            return _routeMatcher.Match(messageRoute, _route);
        }

        public void SessionDisconnected(Guid sessionId)
        {
            _sessionPolicy.RemoveSession(sessionId);
        }

        public void SessionSubscribed(Guid sessionId)
        {
            _sessionPolicy.AddSession(sessionId);
        }

        public void SessionUnSubscribed(Guid sessionId)
        {
            SessionDisconnected(sessionId);
        }

        public void OnMessageAck(Guid sessionId, Guid messageId)
        {
            _dispatcher.Release(messageId, sessionId);
            _messageStore.DeleteAsync(messageId);
        }

        public void OnMessageNack(Guid sessionId, Guid messageId)
        {
            _dispatcher.Release(messageId, sessionId);
            _queue.Writer.TryWrite(messageId);
        }

        private void OnMessageSentSuccess(Guid sessionId, Guid messageId, bool clientAutoAck)
        {
            if (clientAutoAck)
            {
                OnMessageAck(sessionId, messageId);
            }
        }

        private void OnMessageSentError(Guid sessionId, Guid messageId)
        {
            OnMessageNack(sessionId, messageId);
        }
    }
}