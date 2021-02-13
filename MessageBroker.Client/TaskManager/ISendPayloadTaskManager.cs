using System;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.TaskManager
{
    public interface ISendPayloadTaskManager
    {
        Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge);
        void OnPayloadOkResult(Guid payloadId);
        void OnPayloadErrorResult(Guid payloadId, string error);
        void OnPayloadSendSuccess(Guid payloadId);
        void OnPayloadSendFailed(Guid payloadId);
    }
}