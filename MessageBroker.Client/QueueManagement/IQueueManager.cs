using System;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.QueueManagement
{
    public interface IQueueManager
    {
        event Action<QueueConsumerMessage> MessageReceived;
        void Setup(string name, string route);
        Task<SendAsyncResult> DeclareQueue();
        Task<SendAsyncResult> DeleteQueue();
        Task<SendAsyncResult> SubscribeQueue();
        Task<SendAsyncResult> UnSubscribeQueue();
    }
}