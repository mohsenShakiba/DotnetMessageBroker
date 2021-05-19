using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Models;
using MessageBroker.Common.Models;

namespace MessageBroker.Client.TaskManager
{
    /// <summary>
    /// Will provide a mechanism for awaiting status of payloads that need to be acknowledged by the broker server.
    /// being acknowledged by broker server means that either <see cref="Ok" /> or <see cref="Error" /> is received
    /// and for some payloads we need not to only send the payload but also wait for the acknowledgment.
    /// for such payloads we return a task that completes when either <see cref="Ok" /> or <see cref="Error" /> is
    /// received.
    /// for other types of payload that we don't need to wait for acknowledgment we can complete the task when the payload
    /// has been sent.
    /// </summary>
    public interface ITaskManager : IDisposable
    {
        /// <summary>
        /// Will return a task that will complete when message is sent to server, or acknowledged by server based on wether
        /// the completeOnAcknowledge is true or false
        /// </summary>
        /// <param name="id">Identifier of the payload</param>
        /// <param name="completeOnAcknowledge">
        /// If true will wait until payload is acknowledged by the server, otherwise will wait until the message is sent
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken" /> used to async operations</param>
        /// <returns>Returns a task containing the result of payload send process</returns>
        Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge, CancellationToken cancellationToken);

        /// <summary>
        /// Called once the broker server acknowledged the request by sending <see cref="Ok" />
        /// </summary>
        /// <param name="payloadId">Identifier of the payload</param>
        void OnPayloadOkResult(Guid payloadId);

        /// <summary>
        /// Called once the broker server acknowledged the request by sending <see cref="Error" />
        /// </summary>
        /// <param name="payloadId">Identifier of the payload</param>
        /// <param name="error">Error message of the request</param>
        void OnPayloadErrorResult(Guid payloadId, string error);

        /// <summary>
        /// Called once the payload has been sent to broker server
        /// </summary>
        /// <param name="payloadId">Identifier of the payload</param>
        void OnPayloadSendSuccess(Guid payloadId);

        /// <summary>
        /// Called if sending data to server fails
        /// </summary>
        /// <param name="payloadId">Identifier of the payload</param>
        void OnPayloadSendFailed(Guid payloadId);
    }
}