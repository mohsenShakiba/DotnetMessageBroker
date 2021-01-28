namespace MessageBroker.Core.StatRecording
{
    public interface IStatRecorder
    {
        void OnMessageReceived();
        void OnMessageSent();
        
        int MessageReceived { get; }
        int MessageSent { get; }
    }
}