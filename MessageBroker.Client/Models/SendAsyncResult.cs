namespace MessageBroker.Client.Models
{
    public class SendAsyncResult
    {
        public bool IsSuccess { get; init; }
        public string InternalErrorCode { get; init; }

        public static SendAsyncResult AlreadyCompleted => new()
        {
            IsSuccess = false,
            InternalErrorCode = "Already completed, cannot re-process"
        };
    }
}