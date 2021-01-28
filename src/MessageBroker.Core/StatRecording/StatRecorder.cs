using System.Threading;

namespace MessageBroker.Core.StatRecording
{
    public class StatRecorder : IStatRecorder
    {
        private int _messageReceived;
        private int _messageSent;

        public int MessageReceived => _messageReceived;
        public int MessageSent => _messageSent;

        public void OnMessageReceived()
        {
            Interlocked.Increment(ref _messageReceived);
        }

        public void OnMessageSent()
        {
            Interlocked.Increment(ref _messageSent);
        }
    }
}