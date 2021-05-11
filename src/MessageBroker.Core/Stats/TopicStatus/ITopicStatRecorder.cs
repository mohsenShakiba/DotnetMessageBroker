namespace MessageBroker.Core.Stats.TopicStatus
{
    public interface ITopicStatRecorder
    {
        void OnMessageReceived();
        void OnMessageSent();
        void OnMessageProcessed();
        void OnSubscriptionAdded();
        void OnSubscriptionRemoved();
        
        int ReceivedMessageCount { get; }
        int SentMessageCount { get; }
        int ProcessedMessageCount { get; }
        int SubscriptionCount { get; }
    }
}