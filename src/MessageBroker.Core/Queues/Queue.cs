using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
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
            IMessageStore messageStore, ISendQueueStore sendQueueStore, IRouteMatcher routeMatcher,
            ISerializer serializer)
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
            _messageStore.Add(queueMessage);

            // add the message to queue chan
            _queue.Writer.TryWrite(queueMessage.Id);
        }

        public bool MessageRouteMatch(string messageRoute)
        {
            return _routeMatcher.Match(messageRoute, Route);
        }

        public void SessionDisconnected(Guid sessionId)
        {
            _sessionPolicy.RemoveSendQueue(sessionId);
        }

        public void SessionSubscribed(Guid sessionId)
        {
            if (_sendQueueStore.TryGet(sessionId, out var sendQueue))
            {
                _sessionPolicy.AddSendQueue(sendQueue);
            }
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
        }

        private void SetupSendQueueProcessor()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_stopped)
                {
                    await ReadNextMessage();
                }
                
                Logger.LogInformation("exited queue");
            }, TaskCreationOptions.LongRunning);
        }

        public async Task ReadNextMessage()
        {
            if (_queue.Reader.TryRead(out var messageId))
            {
                await ProcessMessage(messageId);
            }
        }

        private async Task ProcessMessage(Guid messageId)
        {
            if (_messageStore.TryGetValue(messageId, out var message))
            {
                Logger.LogInformation($"sending message with content : {Encoding.UTF8.GetString(message.Data.Span)} and id {message.Id}");
                var sendPayload = _serializer.Serialize(message);

                await FindSendQueueForMessage(sendPayload);

                message.Dispose();
            }
            else
            {
                
            }
        }

        private async Task FindSendQueueForMessage(SerializedPayload serializedPayload)
        {
            while (true)
            {
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                
                    var sendQueue = await _sessionPolicy.GetNextAvailableSendQueueAsync(cts.Token);

                    SendMessage(sendQueue, serializedPayload);
                    break;
                }
                catch
                {
                    
                }
                
            }

        }

        private void SendMessage(ISendQueue sendQueue, SerializedPayload serializedPayload)
        {
            serializedPayload.ClearStatusListener();
            serializedPayload.OnStatusChanged += OnMessageStatusChanged;
            
            sendQueue.Enqueue(serializedPayload);
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
            _messageStore.Delete(messageId);
        }

        private void OnMessageNack(Guid messageId)
        {
            Logger.LogInformation($"nack received in queue for message id {messageId}, retrying");
            _queue.Writer.TryWrite(messageId);
        }

    }
}