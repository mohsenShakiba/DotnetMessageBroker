using MessageBroker.Common;
using MessageBroker.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace MessageBroker.Core
{
    public class Coordinator
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly ILogger<Coordinator> _logger;
        private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers;

        public Coordinator(IMessageProcessor messageProcessor, ILogger<Coordinator> logger)
        {
            _messageProcessor = messageProcessor;
            _logger = logger;
            _subscribers = new();
        }

        public void OnMessage(Message message)
        {
            // find which subscribers should receive this message
            // send to subscribers
            // release the message
        }

        public void OnAck(Ack ack)
        {
        }

        public void OnNack(Ack nack)
        {
        } 

        public void OnListen(Guid sessionId, Listen listen)
        {
            if (_subscribers.TryGetValue(sessionId, out var subscriber))
            {
                subscriber.AddRoute(listen.Route);
            }
            else
            {
                _logger.LogError($"failed to find subscriber with id: {sessionId}");
            }
        }

        public void OnUnListen(Guid sessionId, Listen listen)
        {
            if (_subscribers.TryGetValue(sessionId, out var subscriber))
            {
                subscriber.RemoveRoute(listen.Route);
            }
            else
            {
                _logger.LogError($"failed to find subscriber with id: {sessionId}");
            }
        }

        public void OnClientDisconnected(Guid sessionId)
        {
            _subscribers.TryRemove(sessionId, out _);
        }

    }
}
