using MessageBroker.Core.MessageProcessor;
using MessageBroker.Core.Models;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.Serialize;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MessageBroker.Core
{
    public class Coordinator : IMessageProcessor
    {
        private readonly ISessionResolver _sessionResolver;
        private readonly ISerializer _serializer;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly IRouteMatcher _routeMatcher;
        private readonly ILogger<Coordinator> _logger;
        private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers;
        public int _stat;

        public Coordinator(ISessionResolver sessionResolver, ISerializer serializer, MessageDispatcher messageDispatcher, IRouteMatcher routeMatcher, ILogger<Coordinator> logger)
        {
            _sessionResolver = sessionResolver;
            _serializer = serializer;
            _messageDispatcher = messageDispatcher;
            _routeMatcher = routeMatcher;
            _logger = logger;
            _subscribers = new();
        }

        public void ClientConnected(Guid sessionId)
        {
            // nothing
        }

        public void ClientDisconnected(Guid sessionId)
        {
            _subscribers.TryRemove(sessionId, out _);
        }

        public void DataReceived(Guid sessionId, Memory<byte> data)
        {
            var payload = _serializer.Deserialize(data);

            if (payload == null)
            {
                _logger.LogError($"failed to parse message from publisher: {sessionId}");
            }

            switch (payload)
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
                case Subscribe subscribe:
                    OnSubscribe(sessionId, subscribe);
                    break;
            }
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
            AckRecieved(sessionId, message.Id);
        }

        public void OnAck(Guid sessionId, Ack ack)
        {
            _messageDispatcher.Release(ack.Id, new Guid[1] { sessionId });
        }

        public void OnNack(Guid sessionId, Nack nack)
        {
            _messageDispatcher.Release(nack.Id, new Guid[1] { sessionId });
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

            AckRecieved(sessionId, listen.Id);
        }

        public void OnUnListen(Guid sessionId, Unlisten unlisten)
        {
            if (_subscribers.TryGetValue(sessionId, out var subscriber))
            {
                subscriber.RemoveRoute(unlisten.Route);
            }
            else
            {
                _logger.LogError($"failed to find subscriber with id: {sessionId}");
            }

            AckRecieved(sessionId, unlisten.Id);
        }

        private void OnSubscribe(Guid sessionId, Subscribe subscribe)
        {
            _messageDispatcher.AddSendQueue(sessionId, subscribe.Concurrency);
        }

        private void RegisterSubscriber(Guid sessionId, string route)
        {
            var subscriber = new Subscriber(sessionId);
            subscriber.AddRoute(route);
            _subscribers[sessionId] = subscriber;
        }

        private void AckRecieved(Guid sessionId, Guid payloadId)
        {
            var session = _sessionResolver.Resolve(sessionId);

            if (session == null)
            {
                _logger.LogError($"failed to find a publisher with id: {sessionId}");
                return;
            }

            var ack = new Ack(payloadId);
            var ackData = _serializer.Serialize(ack);

            session.Send(ackData);

        }


    }
}
