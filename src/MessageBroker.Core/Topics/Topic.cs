﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.DynamicThrottling;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Clients;
using MessageBroker.Core.DispatchPolicy;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.Stats.TopicStatus;
using MessageBroker.Models;
using MessageBroker.Models.Async;
using MessageBroker.Serialization;
using MessageBroker.TCP.Binary;

namespace MessageBroker.Core.Topics
{
    /// <inheritdoc />
    public class Topic : ITopic, IAsyncPayloadTicketHandler
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
        private readonly IDispatcher _dispatcher;
        private readonly DynamicWaitThrottling _throttling;

        public string Name { get; private set; }
        public string Route { get; private set; }
        public ITopicStatRecorder StatRecorder { get; }

        private bool _disposed;

        public Topic(IDispatcher dispatcher, IMessageStore messageStore, IRouteMatcher routeMatcher,
            ISerializer serializer)
        {
            _dispatcher = dispatcher;
            _messageStore = messageStore;
            _routeMatcher = routeMatcher;
            _serializer = serializer;
            _queueChannel = Channel.CreateUnbounded<Guid>();
            _throttling = new();
            StatRecorder = new TopicStatRecorder();
        }


        public void Dispose()
        {
            Volatile.Write(ref _disposed, true);

            // if called twice
            _ = _queueChannel.Writer.TryComplete();
        }

        public void Setup(string name, string route)
        {
            ThrowIfDisposed();

            Name = name;
            Route = route;

            ReadPayloadsFromMessageStore();
            ReadNextMessagesContinuously();
        }

        public void OnMessage(Message message)
        {
            Logger.LogInformation(
                $"Topic {Name} received message with id: {message.Id} and count {StatRecorder.ReceivedMessageCount}");

            ThrowIfDisposed();

            // create TopicMessage from message
            var queueMessage = message.ToTopicMessage(Name);

            // persist the message
            _messageStore.Add(queueMessage);

            // add the message to queue chan
            _queueChannel.Writer.TryWrite(queueMessage.Id);

            StatRecorder.OnMessageReceived();
        }

        public bool MessageRouteMatch(string messageRoute)
        {
            if (Volatile.Read(ref _disposed))
            {
                return false;
            }

            return _routeMatcher.Match(messageRoute, Route);
        }

        public void ClientSubscribed(IClient client)
        {
            ThrowIfDisposed();
            _dispatcher.Add(client);
        }

        public void ClientUnsubscribed(IClient client)
        {
            ThrowIfDisposed();
            var clientIsSubscribed = _dispatcher.Remove(client);
            if (clientIsSubscribed)
            {
                Logger.LogInformation($"Client removed from topic {Name} with {_queueChannel.Reader.Count}");
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

            foreach (var message in messages)
                _queueChannel.Writer.TryWrite(message);
        }

        private void ReadNextMessagesContinuously()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_disposed)
                {
                    await ReadNextMessage();
                }
            }, TaskCreationOptions.LongRunning);
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

                if (client is null)
                {
                    Logger.LogInformation($"delaying message with id: {serializedPayload.PayloadId}");
                    await _throttling.WaitAndIncrease();
                    continue;
                }

                // reset the _throttling to base value
                _throttling.Reset();

                // get ticket for payload
                try
                {
                    Logger.LogInformation($"Enqueue message with id {serializedPayload.PayloadId}");
                    var ticket = client.Enqueue(serializedPayload);
                    // listen to status of payload
                    ticket.Handler = this;
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
            Logger.LogInformation($"received ack for ticket with id {messageId}");
            _messageStore.Delete(messageId);
        }

        /// <summary>
        /// if message is nacked we should re-queue the message
        /// </summary>
        /// <param name="messageId"></param>
        private void OnMessageNack(Guid messageId)
        {
            Logger.LogInformation($"received nack for ticket with id {messageId}");
            _queueChannel.Writer.TryWrite(messageId);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed))
                throw new ObjectDisposedException($"Topic has been disposed");
        }
    }
}