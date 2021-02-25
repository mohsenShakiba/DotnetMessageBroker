using System;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    public interface ISendQueue
    {
        public int Available { get; }
        public IClientSession Session { get; }

        void Configure(int concurrency, bool autoAck);
        void Enqueue(SerializedPayload serializedPayload);
        void OnMessageAckReceived(Guid messageId);
        void OnMessageNackReceived(Guid messageId);
        void Stop();
    }
}