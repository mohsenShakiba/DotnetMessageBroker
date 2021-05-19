namespace MessageBroker.Client.Models
{
    /// <summary>
    /// Object containing the result of async request to server
    /// </summary>
    public class SendAsyncResult
    {
        /// <summary>
        /// The result of request
        /// </summary>
        /// <remarks>If false check the <see cref="InternalErrorCode" /></remarks>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message containing a description of what went wrong
        /// </summary>
        public string InternalErrorCode { get; set; }
    }
}