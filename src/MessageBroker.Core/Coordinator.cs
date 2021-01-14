using MessageBroker.Core.MessageProcessor;
using MessageBroker.Core.MessageRefStore;
using MessageBroker.Core.Models;
using MessageBroker.Core.Persistance;
using MessageBroker.Core.Queue;
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
        private readonly ConcurrentDictionary<string, IQueue> _subscribers;
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
            // remove this
            _messageDispatcher.AddSendQueue(sessionId, 1000);
        }

        public void ClientDisconnected(Guid sessionId)
        {
            foreach(var (_, queue) in _subscribers) {
                queue.SessionDisconnected(sessionId);
            }
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
                case PayloadType.QueueCreate:
                    var queueDeclare = _serializer.ToQueueDeclareModel(payloadData);
                    DeclareQueue(sessionId, queueDeclare);
                    break;
                case PayloadType.QueueDelete:
                    var queueDelete = _serializer.ToQueueDeleteModel(payloadData);
                    DeleteQueue(sessionId, queueDelete);
                    break;
            }
        }

        public void OnMessage(Guid sessionId, Message message)
        {
            // send message to all the queues that match this message route
            foreach (var (_, queue) in _subscribers)
                if (queue.MessageRouteMatch(message.Route))
                    queue.OnMessage(message);

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
            if (_subscribers.TryGetValue(listen.QueueName, out var queue))
            {
                queue.SessionSubscribed(sessionId);
                SendRecievedPayloadAck(sessionId, listen.Id);
            }
            else
            {
                SendRecievedPayloadNack(sessionId, listen.Id);
            }
        }

        public void OnUnListen(Guid sessionId, Listen unlisten)
        {
            if (_subscribers.TryGetValue(unlisten.QueueName, out var queue))
            {
                queue.SessionUnSubscribed(sessionId);
                SendRecievedPayloadAck(sessionId, unlisten.Id);
            }
            else
            {
                SendRecievedPayloadNack(sessionId, unlisten.Id);
            }
        }

        private void OnSubscribe(Guid sessionId, Subscribe subscribe)
        {
            _messageDispatcher.AddSendQueue(sessionId, subscribe.Concurrency);
            SendRecievedPayloadAck(sessionId, subscribe.Id);
        }


        private void DeclareQueue(Guid sessionId, QueueDeclare queueDeclare)
        {
            // check if queue exists 
            if (_subscribers.TryGetValue(queueDeclare.Name, out var queue))
            {
                SendRecievedPayloadAck(sessionId, queueDeclare.Id);
                return;
            }

            var sessionSelectionPolicy = new RandomSessionSelectionPolicy();
            queue = new MessageQueue(_messageDispatcher, sessionSelectionPolicy, _messageStore, _routeMatcher);
            queue.Setup(queueDeclare.Name, queueDeclare.Route);

            _subscribers[queueDeclare.Name] = queue;

            SendRecievedPayloadAck(sessionId, queueDeclare.Id);
        }

        private void DeleteQueue(Guid sessionId, QueueDelete queueDelete)
        {
            _subscribers.TryRemove(queueDelete.Name, out var _);
            SendRecievedPayloadAck(sessionId, queueDelete.Id);
        }

        /// <summary>
        /// Ack that is sent to client indicating the payload has been successfully processed
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="payloadId"></param>
        private void SendRecievedPayloadAck(Guid sessionId, Guid payloadId)
        {
            var ack = new Ack
            {
                Id = payloadId
            };

            _messageDispatcher.Dispatch(ack, sessionId);
        }

        private void SendRecievedPayloadNack(Guid sessionId, Guid payloadId)
        {
            var ack = new Ack
            {
                Id = payloadId
            };

            _messageDispatcher.Dispatch(ack, sessionId);
        }
    }
}
