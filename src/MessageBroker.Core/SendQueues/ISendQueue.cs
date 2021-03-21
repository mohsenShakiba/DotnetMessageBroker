using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    public interface ISendQueue
    {
        public Guid Id { get; }
        public bool IsAvailable { get; }
        public int AvailableCount { get; }
        public SendQueueAvailabilityTicket AvailabilityTicket { get; }
        event Action<SendQueueAvailabilityTicket> OnAvailable;

        void ProcessPendingPayloads();
        Task ReadNextPayloadAsync();
        void Configure(int prefetchCount, bool autoAck);
        void Enqueue(SerializedPayload serializedPayload);
        void OnMessageAckReceived(Guid messageId);
        void OnMessageNackReceived(Guid messageId);
        void Stop();
    }
}