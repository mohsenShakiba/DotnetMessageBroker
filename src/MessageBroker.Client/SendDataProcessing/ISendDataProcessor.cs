using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Models;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Models;

namespace MessageBroker.Client.SendDataProcessing
{
    /// <summary>
    /// Utility class for sending data to broker server and optionally wait for response from broker server
    /// </summary>
    public interface ISendDataProcessor
    {
        /// <summary>
        /// Will send data to server and wait for response based on completeOnSeverOkReceived
        /// </summary>
        /// <param name="serializedPayload">Data to be sent</param>
        /// <param name="completeOnSeverOkReceived">
        /// If true then the task is completed when an <see cref="Ok" /> object
        /// or <see cref="Error" /> is received from broker server, otherwise once the data is sent task will be completed
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken" /> used for async operations</param>
        /// <returns>Response of the request, including the status and optional message</returns>
        Task<SendAsyncResult> SendAsync(SerializedPayload serializedPayload, bool completeOnSeverOkReceived,
            CancellationToken cancellationToken);
    }
}