using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.TaskManager
{
    public class SendPayloadTaskManager : ISendPayloadTaskManager
    {
        private readonly ConcurrentDictionary<Guid, SendPayloadTaskCompletionSource> _tasks;

        public SendPayloadTaskManager()
        {
            _tasks = new ConcurrentDictionary<Guid, SendPayloadTaskCompletionSource>();
        }

        public Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge)
        {
            var tcs = new TaskCompletionSource<SendAsyncResult>();

            var data = new SendPayloadTaskCompletionSource
            {
                CompleteOnAcknowledge = completeOnAcknowledge,
                TaskCompletionSource = tcs
            };

            _tasks[id] = data;

            return tcs.Task;
        }

        public void OnPayloadOkResult(Guid payloadId)
        {
            if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
                taskCompletionSource.OnOk();
        }

        public void OnPayloadErrorResult(Guid payloadId, string error)
        {
            if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
                taskCompletionSource.OnError(error);
        }

        public void OnPayloadSendSuccess(Guid payloadId)
        {
            if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
                taskCompletionSource.OnSendSuccess();
        }

        public void OnPayloadSendFailed(Guid payloadId)
        {
            if (_tasks.TryGetValue(payloadId, out var taskCompletionSource))
                taskCompletionSource.OnSendError();
        }
    }
}