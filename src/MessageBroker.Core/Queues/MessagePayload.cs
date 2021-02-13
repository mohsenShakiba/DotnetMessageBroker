using System;
using MessageBroker.Common.Pooling;
using MessageBroker.Serialization;

namespace MessageBroker.Core.Queues
{
    //todo: remove SerializedPayload
    public class MessagePayload : IDisposable
    {
        public SerializedPayload SerializedPayload { get; private set; }
        public bool HasSetupStatusChangeListener { get; private set; }

        public void Dispose()
        {
            ObjectPool.Shared.Return(this);
            ObjectPool.Shared.Return(SerializedPayload);
        }

        public event Action<Guid, MessagePayloadStatus> OnStatusChanged;

        public void Setup(SerializedPayload serializedPayload)
        {
            SerializedPayload = serializedPayload;
        }

        public void SetStatus(MessagePayloadStatus payloadStatus)
        {
            OnStatusChanged?.Invoke(SerializedPayload.Id, payloadStatus);
        }

        public void StatusChangeListenerIsSet()
        {
            HasSetupStatusChangeListener = true;
        }
    }
}