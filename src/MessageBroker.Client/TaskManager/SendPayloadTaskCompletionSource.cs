using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.TaskManager
{
    /// <summary>
    /// Contains logic for completing task returned to client on whether <see cref="CompleteOnAcknowledge" /> is true or
    /// false
    /// </summary>
    public class SendPayloadTaskCompletionSource
    {
        public TaskCompletionSource<SendAsyncResult> TaskCompletionSource { get; set; }
        public bool CompleteOnAcknowledge { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public void OnOk()
        {
            TaskCompletionSource.TrySetResult(new SendAsyncResult
            {
                IsSuccess = true
            });
        }

        public void OnError(string error)
        {
            TaskCompletionSource.TrySetResult(new SendAsyncResult
            {
                IsSuccess = false,
                InternalErrorCode = error
            });
        }

        public void OnSendSuccess()
        {
            if (!CompleteOnAcknowledge)
                OnOk();
        }

        public void OnSendError()
        {
            TaskCompletionSource.TrySetResult(new SendAsyncResult
            {
                IsSuccess = false,
                InternalErrorCode = "Failed to send data to server"
            });
        }
    }
}