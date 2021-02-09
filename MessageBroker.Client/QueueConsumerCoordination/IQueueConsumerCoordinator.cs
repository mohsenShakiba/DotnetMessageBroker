using MessageBroker.Models;

namespace MessageBroker.Client.QueueConsumerCoordination
{
    public interface IQueueConsumerCoordinator
    {
        void Add(QueueManager queueManager);
        void Remove(QueueManager queueManager);
        void OnMessage(QueueMessage queueMessage);
    }
}