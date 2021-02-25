using MessageBroker.Client.QueueManagement;
using MessageBroker.Models;

namespace MessageBroker.Client.QueueConsumerCoordination
{
    public interface IQueueManagerStore
    {
        void Add(QueueManager queueManager);
        void Remove(QueueManager queueManager);
        void OnMessage(QueueMessage queueMessage);
    }
}