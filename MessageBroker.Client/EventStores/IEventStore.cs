using System;

namespace MessageBroker.Client.EventStores
{
    public interface IEventStore
    {
        event Action<ClientSendEvent> OnResult;

        void OnAck(Guid id);
        void OnNack(Guid id);
        void OnSent(Guid id);
    }
}