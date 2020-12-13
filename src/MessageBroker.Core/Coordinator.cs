using MessageBroker.Common;
using System;
using System.Collections.Concurrent;

namespace MessageBroker.Core
{
    public class Coordinator
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers;
        private readonly ConcurrentDictionary<Guid, Publisher> _publishers;

        public Coordinator(IMessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor;
            _subscribers = new();
            _publishers = new();
        }

        public void OnMessage(Messages.Message message)
        {
        }

        public void OnAck(Messages.Ack ack)
        {
        }

        public void OnNack(Messages.Ack nack)
        {
        } 

        public void OnNewPublisher(Messages.RegisterPublisher registerPublisher)
        {
            _publishers[registerPublisher.SessionId] = new Publisher(registerPublisher.SessionId);
        }

        public void OnNewSubscriber(Messages.Register registerSubscriber)
        {
            _subscribers[registerSubscriber.SessionId] = new Subscriber(registerSubscriber.SessionId);
        }

        public void OnClientDisconnected(Guid sessionId)
        {
            _publishers.TryRemove(sessionId, out _);
            _subscribers.TryRemove(sessionId, out _);
        }

    }
}
