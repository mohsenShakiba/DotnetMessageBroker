namespace MessageBroker.Client.Models
{
    public class SendAsyncResult
    {
        public bool IsSuccess { get; set; }
        public string InternalErrorCode { get; set; }

        public static SendAsyncResult AlreadyCompleted => new()
        {
            IsSuccess = true,
            InternalErrorCode = "Already completed, cannot re-process"
        };

        public static SendAsyncResult SocketNotConnected => new()
        {
            IsSuccess = false,
            InternalErrorCode = "Client socket in not in connected state"
        };

        public static SendAsyncResult Error(string error)
        {
            return new()
            {
                IsSuccess = false,
                InternalErrorCode = error
            };
        }
    }
}