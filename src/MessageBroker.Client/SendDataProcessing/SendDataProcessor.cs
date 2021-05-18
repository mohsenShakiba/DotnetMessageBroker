using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;

namespace MessageBroker.Client.SendDataProcessing
{
    public class SendDataProcessor : ISendDataProcessor
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ITaskManager _taskManager;

        public SendDataProcessor(ITaskManager taskManager, IConnectionManager connectionManager)
        {
            _taskManager = taskManager;
            _connectionManager = connectionManager;
        }

        public async Task<SendAsyncResult> SendAsync(SerializedPayload serializedPayload,
            bool completeOnSeverOkReceived, CancellationToken cancellationToken)
        {
            if (completeOnSeverOkReceived)
            {
                var sendPayloadTask =
                    _taskManager.Setup(serializedPayload.PayloadId, true, cancellationToken);

                var sendSuccess = await _connectionManager.SendAsync(serializedPayload, cancellationToken);

                if (sendSuccess)
                    _taskManager.OnPayloadSendSuccess(serializedPayload.PayloadId);
                else
                    _taskManager.OnPayloadSendFailed(serializedPayload.PayloadId);

                return await sendPayloadTask;
            }
            else
            {
                var sendSuccess = await _connectionManager.SendAsync(serializedPayload, cancellationToken);
                return new SendAsyncResult {IsSuccess = sendSuccess};
            }
        }
    }
}