using System;
using System.Threading;

namespace MessageBroker.Client.Models
{
    /// <summary>
    /// Message that is contains data received from topic and can be marked as <see cref="Ack" /> or <see cref="Nack" />
    /// </summary>
    public class SubscriptionMessage
    {
        /// <summary>
        /// Identifier of the message
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Name of the topic it was received from
        /// </summary>
        public string TopicRoute { get; set; }

        /// <summary>
        /// Route of the original message
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// Data included in the original message
        /// </summary>
        public Memory<byte> Data { get; set; }

        internal event Action<Guid, bool, CancellationToken> OnMessageProcessedByClient;

        /// <summary>
        /// Will send an Ack payload to server indicating that message was processed
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken" /> used for async operations</param>
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

        /// <summary>
        /// Will send a Nack payload to server indicating that message was not processed
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken" /> used for async operations</param>
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