using System;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;

namespace MessageBroker.Client.QueueManagement
{
    public class QueueManager : IQueueManager
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IQueueManagerStore _queueManagerStore;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly ISerializer _serializer;

        public QueueManager(ISerializer serializer, IConnectionManager connectionManager,
            ISendPayloadTaskManager sendPayloadTaskManager, IQueueManagerStore queueManagerStore)
        {
            _serializer = serializer;
            _connectionManager = connectionManager;
            _sendPayloadTaskManager = sendPayloadTaskManager;
            _queueManagerStore = queueManagerStore;
        }

        public string Name { get; private set; }

        public string Route { get; private set; }

        public event Action<QueueConsumerMessage> MessageReceived;

        public void Setup(string name, string route)
        {
            Route = route;
            Name = name;
            _queueManagerStore.Add(this);
        }

        public async Task<SendAsyncResult> DeclareQueue()
        {
            var serializedPayload = QueueDeclareSerializedPayload();

            var result = await SendAsync(serializedPayload);

            return result;
        }

        public async Task<SendAsyncResult> DeleteQueue()
        {
            var sendPayload = QueueDeleteSendPayload();

            var result = await SendAsync(sendPayload);

            return result;
        }

        public async Task<SendAsyncResult> SubscribeQueue()
        {
            var sendPayload = SubscribeSendPayload();

            var result = await SendAsync(sendPayload);

            return result;
        }

        public async Task<SendAsyncResult> UnSubscribeQueue()
        {
            var sendPayload = UnSubscribeSendPayload();

            var result = await SendAsync(sendPayload);

            return result;
        }

        private async Task<SendAsyncResult> SendAsync(SerializedPayload serializedPayload)
        {
            var clientSession = _connectionManager.ClientSession;

            var sendPayloadTask = _sendPayloadTaskManager.Setup(serializedPayload.Id, true);

            var sendSuccess = await clientSession.SendAsync(serializedPayload.Data);

            if (sendSuccess)
                _sendPayloadTaskManager.OnPayloadSendSuccess(serializedPayload.Id);
            else
                _sendPayloadTaskManager.OnPayloadSendFailed(serializedPayload.Id);

            return await sendPayloadTask;
        }

        private SerializedPayload QueueDeclareSerializedPayload()
        {
            var payload = new QueueDeclare
            {
                Id = Guid.NewGuid(),
                Name = Name,
                Route = Route
            };

            return _serializer.Serialize(payload);
        }

        private SerializedPayload QueueDeleteSendPayload()
        {
            var payload = new QueueDelete
            {
                Id = Guid.NewGuid(),
                Name = Name
            };

            return _serializer.Serialize(payload);
        }

        private SerializedPayload SubscribeSendPayload()
        {
            var payload = new SubscribeQueue
            {
                Id = Guid.NewGuid(),
                QueueName = Name
            };

            return _serializer.Serialize(payload);
        }

        private SerializedPayload UnSubscribeSendPayload()
        {
            var payload = new UnsubscribeQueue
            {
                Id = Guid.NewGuid(),
                QueueName = Name
            };

            return _serializer.Serialize(payload);
        }

        internal void OnMessage(QueueMessage queueMessage)
        {
            var queueConsumerMessage = new QueueConsumerMessage
            {
                MessageId = queueMessage.Id,
                Data = queueMessage.Data,
                Route = queueMessage.Route,
                QueueName = queueMessage.QueueName
            };

            MessageReceived?.Invoke(queueConsumerMessage);
        }
    }
}