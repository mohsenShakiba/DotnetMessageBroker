using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models.Binary;

namespace MessageBroker.Client.SendDataProcessing
{
    public class SendDataProcessor: ISendDataProcessor
    {
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly IConnectionManager _connectionManager;

        public SendDataProcessor(ISendPayloadTaskManager sendPayloadTaskManager, IConnectionManager connectionManager)
        {
            _sendPayloadTaskManager = sendPayloadTaskManager;
            _connectionManager = connectionManager;
        }
        
        public async Task<SendAsyncResult> SendAsync(SerializedPayload serializedPayload, bool completeOnSeverOkReceived, CancellationToken cancellationToken)
        {
            if (completeOnSeverOkReceived)
            {
                var sendPayloadTask = _sendPayloadTaskManager.Setup(serializedPayload.PayloadId, true, cancellationToken);

                var sendSuccess = await _connectionManager.SendAsync(serializedPayload, cancellationToken);

                if (sendSuccess)
                    _sendPayloadTaskManager.OnPayloadSendSuccess(serializedPayload.PayloadId);
                else
                    _sendPayloadTaskManager.OnPayloadSendFailed(serializedPayload.PayloadId);

                return await sendPayloadTask;
            }
            else
            {
                var sendSuccess = await _connectionManager.SendAsync(serializedPayload, cancellationToken);
                return new SendAsyncResult{IsSuccess = sendSuccess};
            }
        }
    }
}