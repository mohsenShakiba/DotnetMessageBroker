using System.Threading.Tasks;

namespace MessageBroker.Client.TaskManager
{
    public class PayloadTaskCompletionData
    {
        public TaskCompletionSource<bool> TaskCompletionSource { get; init; }
        public bool CompleteOnAcknowledge { get; set; }


        public void OnSendResult(bool sent)
        {
            if (CompleteOnAcknowledge)
                return;

            TaskCompletionSource.TrySetResult(true);
        }

        public void OnAcknowledgeResult(bool acknowledged)
        {
            if (!CompleteOnAcknowledge)
                return;

            TaskCompletionSource.TrySetResult(acknowledged);
        }
    }
}