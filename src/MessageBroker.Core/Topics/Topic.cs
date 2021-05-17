using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.DynamicThrottling;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Dispatching;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Models;
using MessageBroker.Models.Binary;
using MessageBroker.Serialization;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Core.Topics
{
    /// <inheritdoc />
    public class Topic : ITopic
    {
        /// <summary>
        /// Store for <see cref="Message"/>
        /// reading and writing messages to this store
        /// </summary>
        private readonly IMessageStore _messageStore;

        /// <summary>
        /// used for storing messages waiting to be sent to subscribers
        /// </summary>
        private readonly Channel<Guid> _queueChannel;

        private readonly IRouteMatcher _routeMatcher;
        private readonly ISerializer _serializer;
        private readonly ILogger<Topic> _logger;
        private readonly IDispatcher _dispatcher;
        private readonly DynamicWaitThrottling _throttling;

        public string Name { get; private set; }
        public string Route { get; private set; }

        private bool _disposed;

        public Topic(IDispatcher dispatcher, IMessageStore messageStore, IRouteMatcher routeMatcher,
            ISerializer serializer, ILogger<Topic> logger)
        {
            _dispatcher = dispatcher;
            _messageStore = messageStore;
            _routeMatcher = routeMatcher;
            _serializer = serializer;
            _logger = logger;
            _queueChannel = Channel.CreateUnbounded<Guid>();
            _throttling = new();
        }


        public void Dispose()
        {
            _disposed = true;

            // if called twice
            _ = _queueChannel.Writer.TryComplete();
        }

        public void Setup(string name, string route)
        {
            ThrowIfDisposed();

            Name = name;
            Route = route;

            ReadPayloadsFromMessageStore();
        }
        
        public void StartProcessingMessages()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_disposed)
                {
                    await ReadNextMessage();
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void OnMessage(Message message)
        {
            ThrowIfDisposed();

            _logger.LogInformation($"Topic {Name} received message with id: {message.Id}");

            // create TopicMessage from message
            var queueMessage = message.ToTopicMessage(Name);

            // persist the message
            _messageStore.Add(queueMessage);

            // add the message to queue chan
            _queueChannel.Writer.TryWrite(queueMessage.Id);
        }

        public bool MessageRouteMatch(string messageRoute)
        {
            if (_disposed)
            {
                return false;
            }

            return _routeMatcher.Match(messageRoute, Route);
        }

        public void ClientSubscribed(IClient client)
        {
            _logger.LogInformation($"Added new subscription to topic: {Name} with id: {client.Id}");
            ThrowIfDisposed();
            _dispatcher.Add(client);
        }

        public void ClientUnsubscribed(IClient client)
        {
            ThrowIfDisposed();
            var success = _dispatcher.Remove(client);
            if (success)
            {
                _logger.LogInformation($"Removed subscription from topic: {Name} with id: {client.Id}");
            }
        }

        public async Task ReadNextMessage()
        {
            ThrowIfDisposed();

            if (_queueChannel.Reader.TryRead(out var messageId))
            {
                await ProcessMessage(messageId);
            }
        }

        private void ReadPayloadsFromMessageStore()
        {
            ThrowIfDisposed();

            var messages = _messageStore.GetAll();

            if (messages.Any())
            {
                _logger.LogWarning($"Found {messages.Count()} messages while initializing the topic: {Name}");
            }
            else
            {
                _logger.LogWarning($"No messages was found while initializing the topic: {Name}");
            }

            foreach (var message in messages)
                _queueChannel.Writer.TryWrite(message);
        }

        

        private async Task ProcessMessage(Guid messageId)
        {
            if (_messageStore.TryGetValue(messageId, out var message))
            {
                // convert the message to serialized payload
                var serializedPayload = _serializer.Serialize(message);

                await SendSerializedPayloadToNextAvailableClient(serializedPayload);
            }
        }

        private async ValueTask SendSerializedPayloadToNextAvailableClient(SerializedPayload serializedPayload)
        {
            // keep trying to find an available client 
            while (true)
            {
                var client = _dispatcher.NextAvailable();

                // if no subscription is found then just wait
                if (client is null)
                {
                    await _throttling.WaitAndIncrease();
                    continue;
                }

                // reset the _throttling to base value
                _throttling.Reset();

                // get ticket for payload
                try
                {
                    _logger.LogInformation($"Adding message with id: {serializedPayload.PayloadId} to subscription with id: {client.Id} in topic: {Name}");
                    var ticket = client.Enqueue(serializedPayload);
                    ticket.OnStatusChanged += OnStatusChanged;
                    break;
                }
                catch (ChannelClosedException)
                {
                    // no-op since the client might be disposed
                }
            }
        }

        public void OnStatusChanged(Guid messageId, bool ack)
        {
            if (ack)
            {
                OnMessageAck(messageId);
            }
            else
            {
                OnMessageNack(messageId);
            }
        }

        /// <summary>
        /// if message is acked we can safely delete it
        /// </summary>
        /// <param name="messageId">identifier of the message</param>
        private void OnMessageAck(Guid messageId)
        {
            _logger.LogInformation($"Received ack for message with id: {messageId} in topic: {Name}");
            _messageStore.Delete(messageId);
        }

        /// <summary>
        /// if message is nacked we should re-queue the message
        /// </summary>
        /// <param name="messageId"></param>
        private void OnMessageNack(Guid messageId)
        {
            _logger.LogInformation($"Received Nack for message with id: {messageId} in topic: {Name}");
            _queueChannel.Writer.TryWrite(messageId);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed))
                throw new ObjectDisposedException($"Topic has been disposed");
        }
    }
}