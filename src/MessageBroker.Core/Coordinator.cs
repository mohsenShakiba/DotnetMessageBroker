using System;
using System.Collections.Concurrent;

namespace MessageBroker.Core
{
    public class Coordinator
    {
        private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers;
        private readonly ConcurrentDictionary<Guid, Publisher> _publishers;

        public void OnMessage(Messages.Message message)
        {

        }

        public void OnAck(Messages.Ack ack)
        {
        }

        public void OnNack(Messages.Nack nack)
        {
        } 

        public void OnNewPublisher(Messages.RegisterPublisher registerPublisher)
        {
            _publishers[registerPublisher.SessionId] = new Publisher(registerPublisher.SessionId);
        }

        public void OnNewSubscriber(Messages.RegisterSubscriber registerSubscriber)
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
