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
        private readonly ISessionResolver _sessionResolver;
        private readonly Parser _parser;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly IRouteMatcher _routeMatcher;
        private readonly ILogger<Coordinator> _logger;
        private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers;
        public int _stat;

        public Coordinator(IMessageProcessor messageProcessor, ISessionResolver sessionResolver, Parser parser, MessageDispatcher messageDispatcher, IRouteMatcher routeMatcher, ILogger<Coordinator> logger)
        {
            _messageProcessor = messageProcessor;
            _sessionResolver = sessionResolver;
            _parser = parser;
            _messageDispatcher = messageDispatcher;
            _routeMatcher = routeMatcher;
            _logger = logger;
            _subscribers = new();

            messageProcessor.OnMessageReceived += OnMessageRecieved;

            messageProcessor.OnClientDisconnected += OnClientDisconnected;
        }

        public void OnMessage(Guid sessionId, Message message)
        {
            // find which subscribers should receive this message
            var listOfSubscribersWhichShouldReceiveThisMessage = new List<Guid>();

            foreach (var (_, subscriber) in _subscribers)
                if (subscriber.MatchRoute(message.Route, _routeMatcher))
                    listOfSubscribersWhichShouldReceiveThisMessage.Add(subscriber.SessionId);

            if (listOfSubscribersWhichShouldReceiveThisMessage.Count == 0)
                return;

            // send to subscribers
            _messageDispatcher.Dispatch(message, listOfSubscribersWhichShouldReceiveThisMessage);

            // send received ack to publisher
            var publisher = _sessionResolver.ResolveSession(sessionId);

            if (publisher == null)
                _logger.LogError($"failed to find a publisher with id: {sessionId}");

            var ack = new Ack(message.Id);
            var ackData = _parser.ToBinary(ack);

            publisher.SendSync(ackData);
        }

        public void OnAck(Guid sessionId, Ack ack)
        {
            _messageDispatcher.Release(ack, new Guid[1] { sessionId });
        }

        public void OnNack(Guid sessionId, Nack nack)
        {
            //_messageDispatcher.Release(nack, new Guid[1] { sessionId });
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

        public void OnUnListen(Guid sessionId, Unlisten listen)
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

        private void OnMessageRecieved(Guid sessionId, Memory<byte> data)
        {
            var o = _parser.Parse(data.Span);
            switch (o)
            {
                case Message message:
                    OnMessage(sessionId, message);
                    _stat += 1;
                    break;
                case Ack ack:
                    OnAck(sessionId, ack);
                    break;
                case Nack nack:
                    OnNack(sessionId, nack);
                    break;
                case Listen listen:
                    OnListen(sessionId, listen);
                    break;
                case Unlisten unlisten:
                    OnUnListen(sessionId, unlisten);
                    break;
            }
        }

        private void RegisterSubscriber(Guid sessionId, string route)
        {
            var subscriber = new Subscriber(sessionId);
            subscriber.AddRoute(route);
            _subscribers[sessionId] = subscriber;
        }
    }
}
