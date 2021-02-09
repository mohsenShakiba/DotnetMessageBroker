using System;
using System.Threading.Tasks;
using MessageBroker.Client.EventStores;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.TaskManager
{
    public interface ITaskManager
    {
        Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge);
        void OnPayloadEvent(Guid payloadId, SendEventType ev, string error);
    }
}