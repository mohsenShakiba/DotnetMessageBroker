using System.Threading;
using MessageBroker.Common.Logging;

namespace MessageBroker.Core.Stats.TopicStatus
{
    public class TopicStatRecorder: ITopicStatRecorder
    {

        private int _receivedMessageCount;
        private int _sentMessageCount;
        private int _processedMessageCount;
        private int _subscriptionCount;
        
        
        public void OnMessageReceived()
        {
            Interlocked.Increment(ref _receivedMessageCount);
            Logger.LogInformation($"OnMessageReceived was called with count {_receivedMessageCount}");
        }

        public void OnMessageSent()
        {
            throw new System.NotImplementedException();
        }

        public void OnMessageProcessed()
        {
            throw new System.NotImplementedException();
        }

        public void OnSubscriptionAdded()
        {
            throw new System.NotImplementedException();
        }

        public void OnSubscriptionRemoved()
        {
            throw new System.NotImplementedException();
        }

        public int ReceivedMessageCount => _receivedMessageCount;
        public int SentMessageCount => _sentMessageCount;
        public int ProcessedMessageCount => _processedMessageCount;
        public int SubscriptionCount => _subscriptionCount;
    }
}