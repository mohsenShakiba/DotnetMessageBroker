using System;
using System.Threading;

namespace MessageBroker.Client.Models
{
    public class SubscriptionMessage
    {
        public Guid MessageId { get; set; }
        public string TopicRoute { get; set; }
        public string TopicName { get; set; }
        public Memory<byte> Data { get; set; }
        internal event Action<Guid, bool, CancellationToken> OnMessageProcessedByClient;

        public void Ack(CancellationToken? cancellationToken = null)
        {
            CancellationToken token;

            if (cancellationToken is null)
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                token = cancellationTokenSource.Token;
            }
            else
            {
                token = cancellationToken.Value;
            }

            OnMessageProcessedByClient?.Invoke(MessageId, true, token);
        }

        public void Nack(CancellationToken? cancellationToken = null)
        {
            CancellationToken token;

            if (cancellationToken is null)
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                token = cancellationTokenSource.Token;
            }
            else
            {
                token = cancellationToken.Value;
            }

            OnMessageProcessedByClient?.Invoke(MessageId, false, token);
        }
    }
}