using System;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.Subscription
{
    public interface ISubscriber: IDisposable
    {
        event Action<QueueConsumerMessage> MessageReceived;
        event Action SubscriptionFailed; 
        void Setup(string name, string route);
        Task<SendAsyncResult> DeclareQueue();
        Task<SendAsyncResult> DeleteQueue();
        Task<SendAsyncResult> SubscribeQueue();
        Task<SendAsyncResult> UnSubscribeQueue();
    }
}