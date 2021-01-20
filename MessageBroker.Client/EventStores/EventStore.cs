using System;

namespace MessageBroker.Client.EventStores
{
    public class EventStore : IEventStore
    {
        public event Action<ClientSendEvent> OnResult;

        public void OnAck(Guid id)
        {
            OnResult?.Invoke(new ClientSendEvent
            {
                EventType = SendEventType.Ack,
                Id = id
            });
        }

        public void OnNack(Guid id)
        {
            OnResult?.Invoke(new ClientSendEvent
            {
                EventType = SendEventType.Nack,
                Id = id
            });
        }

        public void OnSent(Guid id)
        {
            OnResult?.Invoke(new ClientSendEvent
            {
                EventType = SendEventType.Sent,
                Id = id
            });
        }
    }
}