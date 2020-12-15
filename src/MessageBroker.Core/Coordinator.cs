using MessageBroker.Common;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MessageBroker.Core
{
    public class Coordinator
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly IRouteMatcher _routeMatcher;
        private readonly ILogger<Coordinator> _logger;
        private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers;

        public Coordinator(IMessageProcessor messageProcessor, MessageDispatcher messageDispatcher, IRouteMatcher routeMatcher, ILogger<Coordinator> logger)
        {
            _messageProcessor = messageProcessor;
            _messageDispatcher = messageDispatcher;
            _routeMatcher = routeMatcher;
            _logger = logger;
            _subscribers = new();
        }

        public void OnMessage(Message message)
        {
            // find which subscribers should receive this message
            var listOfSubscribersWhichShouldReceiveThisMessage = new List<Guid>();

            foreach (var (_, subscriber) in _subscribers)
                if (subscriber.MatchRoute(message.Route, _routeMatcher))
                    listOfSubscribersWhichShouldReceiveThisMessage.Add(subscriber.SessionId);

            if (listOfSubscribersWhichShouldReceiveThisMessage.Count == 0)
                return;

            // send to subscribers
            var destination = new MessageDestination
            {
                Data = message,
                Destinations = listOfSubscribersWhichShouldReceiveThisMessage
            };

            _messageDispatcher.Dispatch(destination);
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
                RegisterSubscriber(sessionId, listen.Route);
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

        private void RegisterSubscriber(Guid sessionId, string route)
        {
            var subscriber = new Subscriber(sessionId);
            subscriber.AddRoute(route);
            _subscribers[sessionId] = subscriber;
        }
    }
}
