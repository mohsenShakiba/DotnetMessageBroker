using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.TaskManager
{
    /// <inheritdoc />
    public class SendPayloadTaskManager : ISendPayloadTaskManager
    {
        private readonly ConcurrentDictionary<Guid, SendPayloadTaskCompletionSource> _tasks;
        private bool _disposed;

        public SendPayloadTaskManager()
        {
            _tasks = new ConcurrentDictionary<Guid, SendPayloadTaskCompletionSource>();
            RunTaskCancelledCheckProcess();
        }

        public Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<SendAsyncResult>();

            var data = new SendPayloadTaskCompletionSource
            {
                CompleteOnAcknowledge = completeOnAcknowledge,
                TaskCompletionSource = tcs,
                CancellationToken = cancellationToken
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

        private void RunTaskCancelledCheckProcess()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_disposed)
                {
                    await Task.Delay(1000);
                    DisposeCancelledTasks();
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void DisposeCancelledTasks()
        {
            foreach (var (_, source) in _tasks)
            {
                if (source.CancellationToken.IsCancellationRequested)
                {
                    source.OnError("Task was cancelled");
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}