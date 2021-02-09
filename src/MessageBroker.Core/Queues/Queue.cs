using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.Persistence;
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
        private readonly ISerializer _serializer;
        private readonly ISessionPolicy _sessionPolicy;
        private readonly Channel<Guid> _queue;

        private string _name;
        private string _route;
        private bool _stopped;

        public string Name => _name;
        public string Route => _route;

        public Queue(MessageDispatcher dispatcher, ISessionPolicy sessionPolicy,
            IMessageStore messageStore, IRouteMatcher routeMatcher, ISerializer serializer)
        {
            _dispatcher = dispatcher;
            _sessionPolicy = sessionPolicy;
            _messageStore = messageStore;
            _routeMatcher = routeMatcher;
            _serializer = serializer;
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

            ReadPayloadsFromMessageStore();
            SetupSendQueueProcessor();
        }

        private void ReadPayloadsFromMessageStore()
        {
            var messages = _messageStore.PendingMessages(int.MaxValue);

            foreach (var message in messages)
                _queue.Writer.TryWrite(message);
            
            Logger.LogInformation($"Queue: setting up messages, found {messages.Count()}");            
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
            }, TaskCreationOptions.LongRunning);
        }

        private void ProcessMessage(Guid messageId)
        {
            if (_messageStore.TryGetValue(messageId, out var message))
            {
                var sendPayload = _serializer.Serialize(message);
                
                SendMessage(sendPayload);
                
                message.Dispose();
            }
        }

        public void OnMessage(Message message)
        {
            // create queue message from message
            var queueMessage = message.ToQueueMessage(Name);
            
            // persist the message
            _messageStore.InsertAsync(queueMessage);
            
            // add the message to queue chan
            _queue.Writer.TryWrite(queueMessage.Id);
        }

        private void SendMessage(SerializedPayload serializedPayload)
        {
            var sessionId = _sessionPolicy.GetNextSession();

            if (sessionId.HasValue)
            {
                var queueMessage = ObjectPool.Shared.Rent<MessagePayload>();

                if (!queueMessage.HasSetupStatusChangeListener)
                {
                    queueMessage.OnStatusChanged += OnMessageStatusChanged;
                    queueMessage.StatusChangeListenerIsSet();
                }
                
                queueMessage.Setup(serializedPayload);
                
                _dispatcher.Dispatch(queueMessage, sessionId.Value);
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
        
        private void OnMessageStatusChanged(Guid messageId, MessagePayloadStatus payloadStatus)
        {
            switch (payloadStatus)
            {
                case MessagePayloadStatus.Ack:
                    OnMessageAck(messageId);
                    break;
                case MessagePayloadStatus.Nack:
                    OnMessageNack(messageId);
                    break;
            }
        }

        private void OnMessageAck(Guid messageId)
        {
            _messageStore.DeleteAsync(messageId);
        }

        private void OnMessageNack(Guid messageId)
        {
            _queue.Writer.TryWrite(messageId);
        }

    }
}