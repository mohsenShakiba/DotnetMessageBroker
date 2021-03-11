using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Exceptions;
using MessageBroker.Client.Models;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;

namespace MessageBroker.Client.Subscription
{
    public class Subscriber : ISubscriber
    {
        private readonly ISerializer _serializer;
        private readonly IConnectionManager _connectionManager;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;

        private bool _disposed;

        public event Action<QueueConsumerMessage> MessageReceived;
        public string Name { get; private set; }
        public string Route { get; private set; }


        public Subscriber(ISerializer serializer, IConnectionManager connectionManager,
            ISendPayloadTaskManager sendPayloadTaskManager)
        {
            _serializer = serializer;
            _connectionManager = connectionManager;
            _sendPayloadTaskManager = sendPayloadTaskManager;
        }

        internal async Task SetupAsync(string name, string route)
        {
            Name = name;
            Route = route;

            await SubscribeAsync();

            _connectionManager.OnConnected += OnClientConnected;
            _connectionManager.OnDisconnected += OnClientDisconnected;
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

        private async Task SubscribeAsync()
        {
            var serializedPayload = GetSubscribeQueueData();
            var result = await SendAsync(serializedPayload);

            if (!result.IsSuccess)
                throw new Exception("Failed to connect to server");
        }

        private async Task UnSubscribeAsync()
        {
            var serializedPayload = GetUnSubscribeQueueData();
            await SendAsync(serializedPayload);
        }

        private async Task<SendAsyncResult> SendAsync(SerializedPayload serializedPayload)
        {
            try
            {
                var clientSession = _connectionManager.ClientSession;

                await _connectionManager.WaitForReadyAsync(CancellationToken.None);
            
                var sendPayloadTask = _sendPayloadTaskManager.Setup(serializedPayload.Id, true);

                var sendSuccess = await clientSession.SendAsync(serializedPayload.Data);

                if (sendSuccess)
                    _sendPayloadTaskManager.OnPayloadSendSuccess(serializedPayload.Id);
                else
                    _sendPayloadTaskManager.OnPayloadSendFailed(serializedPayload.Id);

                return await sendPayloadTask;
            }
            catch (SocketNotConnectionException)
            {
                return SendAsyncResult.SocketNotConnected;
            }
        }

        private SerializedPayload GetSubscribeQueueData()
        {
            var payload = new SubscribeQueue()
            {
                Id = Guid.NewGuid(),
                QueueName = Name,
                Concurrency = 10
            };

            return _serializer.Serialize(payload);
        }

        private SerializedPayload GetUnSubscribeQueueData()
        {
            var payload = new UnsubscribeQueue()
            {
                Id = Guid.NewGuid(),
                QueueName = Name
            };

            return _serializer.Serialize(payload);
        }

        private void OnClientConnected()
        {
            SubscribeAsync()
                .Wait();
        }

        private void OnClientDisconnected()
        {
            // do nothing
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Subscriber));

            _disposed = true;

            MessageReceived = null;

            _connectionManager.OnConnected -= OnClientConnected;
            _connectionManager.OnDisconnected -= OnClientDisconnected;

            await UnSubscribeAsync();
        }
    }
}