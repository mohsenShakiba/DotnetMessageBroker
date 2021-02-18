using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.SessionPolicy;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;

namespace MessageBroker.Core.Queues
{
    public class Queue : IQueue, IDisposable
    {
        private readonly IMessageStore _messageStore;
        private readonly ISendQueueStore _sendQueueStore;
        private readonly Channel<Guid> _queue;
        private readonly IRouteMatcher _routeMatcher;
        private readonly ISerializer _serializer;
        private readonly ISessionPolicy _sessionPolicy;

        private bool _stopped;

        public Queue(ISessionPolicy sessionPolicy,
            IMessageStore messageStore, ISendQueueStore sendQueueStore, IRouteMatcher routeMatcher, ISerializer serializer)
        {
            _sessionPolicy = sessionPolicy;
            _messageStore = messageStore;
            _sendQueueStore = sendQueueStore;
            _routeMatcher = routeMatcher;
            _serializer = serializer;
            _queue = Channel.CreateUnbounded<Guid>();
        }

        public void Dispose()
        {
            _stopped = true;
        }

        public string Name { get; private set; }

        public string Route { get; private set; }

        public void Setup(string name, string route)
        {
            Name = name;
            Route = route;

            ReadPayloadsFromMessageStore();
            SetupSendQueueProcessor();
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

        public bool MessageRouteMatch(string messageRoute)
        {
            return _routeMatcher.Match(messageRoute, Route);
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

                    await foreach (var messageId in messageIds) ProcessMessage(messageId);
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

        private void SendMessage(SerializedPayload serializedPayload)
        {
            var sessionId = _sessionPolicy.GetNextSession();

            if (sessionId.HasValue && _sendQueueStore.TryGet(sessionId.Value, out var sendQueue))
            {
                serializedPayload.ClearStatusListener();
                serializedPayload.OnStatusChanged += OnMessageStatusChanged;
                
                sendQueue.Enqueue(serializedPayload);
            }
        }

        private void OnMessageStatusChanged(Guid messageId, SerializedPayloadStatusUpdate payloadStatusUpdate)
        {
            switch (payloadStatusUpdate)
            {
                case SerializedPayloadStatusUpdate.Ack:
                    OnMessageAck(messageId);
                    break;
                case SerializedPayloadStatusUpdate.Nack:
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