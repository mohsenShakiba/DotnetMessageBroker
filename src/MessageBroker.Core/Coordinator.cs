using MessageBroker.Core.MessageProcessor;
using MessageBroker.Core.MessageRefStore;
using MessageBroker.Core.Models;
using MessageBroker.Core.Persistance;
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
        private readonly IMessageStore _messageStore;
        private readonly IMessageRefStore _messageRefStore;
        private readonly ILogger<Coordinator> _logger;
        private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers;
        public int _stat;

        public Coordinator(ISessionResolver sessionResolver, ISerializer serializer, MessageDispatcher messageDispatcher, 
            IRouteMatcher routeMatcher, IMessageStore messageStore, IMessageRefStore messageRefStore, ILogger<Coordinator> logger)
        {
            _sessionResolver = sessionResolver;
            _serializer = serializer;
            _messageDispatcher = messageDispatcher;
            _routeMatcher = routeMatcher;
            _messageStore = messageStore;
            _messageRefStore = messageRefStore;
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
            var type = _serializer.ParsePayloadType(data);
            var payloadData = data.Span;

            switch (type)
            {
                case PayloadType.Msg:
                    var message = _serializer.ToMessage(payloadData);
                    OnMessage(sessionId, message);
                    _stat += 1;
                    break;
                case PayloadType.Ack:
                    var ack = _serializer.ToAck(payloadData);
                    OnAck(sessionId, ack);
                    break;
                case PayloadType.Nack:
                    var nack = _serializer.ToAck(payloadData);
                    OnNack(sessionId, nack);
                    break;
                case PayloadType.Listen:
                    var listen = _serializer.ToListenRoute(payloadData);
                    OnListen(sessionId, listen);
                    break;
                case PayloadType.Unlisten:
                    var unListen = _serializer.ToListenRoute(payloadData);
                    OnUnListen(sessionId, unListen);
                    break;
                case PayloadType.Subscribe:
                    var subscribe = _serializer.ToSubscribe(payloadData);
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

            // setup ref count
            _messageRefStore.SetUpRefCounter(message.Id, listOfSubscribersWhichShouldReceiveThisMessage.Count);

            // send to subscribers
            _messageDispatcher.Dispatch(message, listOfSubscribersWhichShouldReceiveThisMessage);

            // send received ack to publisher
            SendRecievedPayloadAck(sessionId, message.Id);
        }

        public void OnAck(Guid sessionId, Ack ack)
        {
            _messageDispatcher.Release(ack.Id, new Guid[1] { sessionId });
        }

        public void OnNack(Guid sessionId, Ack nack)
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

            SendRecievedPayloadAck(sessionId, listen.Id);
        }

        public void OnUnListen(Guid sessionId, Listen unlisten)
        {
            if (_subscribers.TryGetValue(sessionId, out var subscriber))
            {
                subscriber.RemoveRoute(unlisten.Route);
            }
            else
            {
                _logger.LogError($"failed to find subscriber with id: {sessionId}");
            }

            SendRecievedPayloadAck(sessionId, unlisten.Id);
        }

        private void OnSubscribe(Guid sessionId, Subscribe subscribe)
        {
            _messageDispatcher.AddSendQueue(sessionId, subscribe.Concurrency);
            SendRecievedPayloadAck(sessionId, subscribe.Id);
        }

        private void RegisterSubscriber(Guid sessionId, string route)
        {
            var subscriber = new Subscriber(sessionId);
            subscriber.AddRoute(route);
            _subscribers[sessionId] = subscriber;
        }

        /// <summary>
        /// Ack that is sent to client indicating the payload has been successfully processed
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="payloadId"></param>
        private void SendRecievedPayloadAck(Guid sessionId, Guid payloadId)
        {
            var session = _sessionResolver.Resolve(sessionId);

            if (session == null)
            {
                _logger.LogError($"failed to find a publisher with id: {sessionId}");
                return;
            }

            var ack = new Ack
            {
                Id = payloadId
            };

            var ackData = _serializer.ToSendPayload(ack);

            session.Send(ackData.Data);

        }


    }
}
