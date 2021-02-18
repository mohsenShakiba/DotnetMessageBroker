using System;
using MessageBroker.Common.Pooling;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;

namespace MessageBroker.Core.Queues
{
    // //todo: remove SerializedPayload
    // public class MessagePayload : IDisposable
    // {
    //     public BinaryPayload BinaryPayload { get; set; }
    //     public bool HasSetupStatusChangeListener { get; private set; }
    //
    //     public void Dispose()
    //     {
    //         ObjectPool.Shared.Return(this);
    //         ObjectPool.Shared.Return(BinaryPayload);
    //     }
    //
    //     public event Action<Guid, MessagePayloadStatus> OnStatusChanged;
    //
    //     public void Setup(BinaryPayload binaryPayload)
    //     {
    //         BinaryPayload = binaryPayload;
    //     }
    //
    //     public void SetStatus(MessagePayloadStatus payloadStatus)
    //     {
    //         OnStatusChanged?.Invoke(BinaryPayload.Id, payloadStatus);
    //     }
    //
    //     public void StatusChangeListenerIsSet()
    //     {
    //         HasSetupStatusChangeListener = true;
    //     }
    // }
}