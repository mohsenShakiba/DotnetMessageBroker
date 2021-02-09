using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.TaskManager
{
    public class SendTaskCompletionSource
    {
        public TaskCompletionSource<SendAsyncResult> TaskCompletionSource { get; init; }
        public bool CompleteOnAcknowledge { get; set; }


        public void OnSendResult(bool sent, string error)
        {
            if (CompleteOnAcknowledge)
                return;

            TaskCompletionSource.TrySetResult(new SendAsyncResult
            {
                IsSuccess = sent,
                InternalErrorCode = error
            });
        }

        public void OnAcknowledgeResult(bool acknowledged, string error)
        {
            if (!CompleteOnAcknowledge)
                return;

            TaskCompletionSource.TrySetResult(new SendAsyncResult
            {
                IsSuccess = acknowledged,
                InternalErrorCode = error
            });
        }
    }
}