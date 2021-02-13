using System;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManager;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Client.QueueManagement
{
    public class QueueManager : IQueueManager
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IQueueConsumerCoordinator _queueConsumerCoordinator;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly ISerializer _serializer;


        private bool _queueDeclared;
        private bool _queueSubscribed;

        public QueueManager(ISerializer serializer, IConnectionManager connectionManager,
            ISendPayloadTaskManager sendPayloadTaskManager, IQueueConsumerCoordinator queueConsumerCoordinator)
        {
            _serializer = serializer;
            _connectionManager = connectionManager;
            _sendPayloadTaskManager = sendPayloadTaskManager;
            _queueConsumerCoordinator = queueConsumerCoordinator;
        }

        public string Name { get; private set; }

        public string Route { get; private set; }

        public event Action<QueueConsumerMessage> MessageReceived;

        public void Setup(string name, string route)
        {
            Route = route;
            Name = name;
            _queueConsumerCoordinator.Add(this);
        }

        public async Task<SendAsyncResult> DeclareQueue()
        {
            if (_queueDeclared)
                return SendAsyncResult.AlreadyCompleted;

            var sendPayload = QueueDeclareSendPayload();

            var result = await SendAsync(sendPayload);

            if (result.IsSuccess) _queueDeclared = true;

            return result;
        }

        public async Task<SendAsyncResult> DeleteQueue()
        {
            var sendPayload = QueueDeleteSendPayload();

            var result = await SendAsync(sendPayload);

            if (result.IsSuccess)
            {
                _queueDeclared = false;
                _queueSubscribed = false;
            }

            return result;
        }

        public async Task<SendAsyncResult> SubscribeQueue()
        {
            if (_queueSubscribed)
                return SendAsyncResult.AlreadyCompleted;

            var sendPayload = SubscribeSendPayload();

            var result = await SendAsync(sendPayload);

            if (result.IsSuccess)
                _queueSubscribed = true;

            return result;
        }

        public async Task<SendAsyncResult> UnSubscribeQueue()
        {
            if (!_queueSubscribed)
                return SendAsyncResult.AlreadyCompleted;

            var sendPayload = UnSubscribeSendPayload();

            var result = await SendAsync(sendPayload);

            if (result.IsSuccess)
                _queueSubscribed = false;

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

        private SerializedPayload QueueDeclareSendPayload()
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