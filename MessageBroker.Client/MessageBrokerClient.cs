using System;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Exceptions;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.Subscription;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Logging;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Client
{
    public class MessageBrokerClient: IAsyncDisposable
    {

        public event Action OnConnected;
        public event Action OnDisconnected;
        
        private readonly IConnectionManager _connectionManager;
        private readonly ISubscriberStore _subscriberStore;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly ISerializer _serializer;

        private bool _isDisposed;

        public bool Connected => _connectionManager.Connected;

        public MessageBrokerClient(ISerializer serializer, ISendPayloadTaskManager sendPayloadTaskManager,
            IConnectionManager connectionManager, ISubscriberStore subscriberStore)
        {
            _serializer = serializer;
            _sendPayloadTaskManager = sendPayloadTaskManager;
            _connectionManager = connectionManager;
            _subscriberStore = subscriberStore;
            
            _connectionManager.OnConnected += OnClientConnected;
            _connectionManager.OnDisconnected += OnClientDisconnected;
        }


        public void Connect(IPEndPoint ipEndPoint)
        {
            _connectionManager.Connect(ipEndPoint);
        }

        public void Reconnect()
        {
            _connectionManager.Reconnect();
        }

        public void Disconnect()
        {
            _connectionManager.Disconnect();
        }

        public async Task<ISubscriber> GetQueueSubscriber(string name, string route)
        {
            var subscriber = new Subscriber(_serializer, _connectionManager, _sendPayloadTaskManager);
            await subscriber.SetupAsync(name, route);
            _subscriberStore.Add(subscriber);
            return subscriber;
        }

        public Task<SendAsyncResult> PublishAsync(string route, byte[] data, bool completedOnAcknowledge = true)
        {
            var sendPayload = GetMessageData(route, data);
            return SendAsync(sendPayload, completedOnAcknowledge);
        }

        public Task<SendAsyncResult> AckAsync(Guid messageId)
        {
            var sendPayload = GetAckData(messageId);
            return SendAsync(sendPayload, false);
        }

        public Task<SendAsyncResult> NackAsync(Guid messageId)
        {
            var sendPayload = GetNackData(messageId);
            return SendAsync(sendPayload, false);
        }

        public Task<SendAsyncResult> ConfigureClientAsync(int concurrency, bool autoAck)
        {
            var sendPayload = GetConfigureSubscriptionData(concurrency, autoAck);
            return SendAsync(sendPayload, true);
        }

        public Task<SendAsyncResult> CreateQueueAsync(string queueName, string queueRoute)
        {
            var serializedPayload = GetDeclareQueueData(queueName, queueRoute);
            return SendAsync(serializedPayload, true);
        }
        
        public Task<SendAsyncResult> RemoveQueueAsync(string queueName)
        {
            var serializedPayload = GetDeleteQueueData(queueName);
            return SendAsync(serializedPayload, true);
        }
        
        private async Task<SendAsyncResult> SendAsync(SerializedPayload serializedPayload, bool completeOnAcknowledge)
        {
            try
            {
                var clientSession = _connectionManager.ClientSession;

                await _connectionManager.WaitForReadyAsync(CancellationToken.None);

                var sendPayloadTask = _sendPayloadTaskManager.Setup(serializedPayload.Id, completeOnAcknowledge);

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

        private SerializedPayload GetMessageData(string route, byte[] data)
        {
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                Data = data,
                Route = route
            };

            return _serializer.Serialize(msg);
        }

        private SerializedPayload GetAckData(Guid messageId)
        {
            var payload = new Ack
            {
                Id = messageId
            };

            return _serializer.Serialize(payload);
        }

        private SerializedPayload GetNackData(Guid messageId)
        {
            var payload = new Nack
            {
                Id = messageId
            };

            return _serializer.Serialize(payload);
        }

        private SerializedPayload GetConfigureSubscriptionData(int concurrency, bool autoAck)
        {
            var payload = new ConfigureSubscription
            {
                Id = Guid.NewGuid(),
                Concurrency = concurrency,
                AutoAck = autoAck
            };

            return _serializer.Serialize(payload);
        }
        
        private SerializedPayload GetDeclareQueueData(string name, string route)
        {
            var payload = new QueueDeclare
            {
                Id = Guid.NewGuid(),
                Name = name,
                Route = route
            };

            return _serializer.Serialize(payload);
        }

        private SerializedPayload GetDeleteQueueData(string name)
        {
            var payload = new QueueDelete
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            return _serializer.Serialize(payload);
        }

        private void OnClientConnected()
        {
            OnConnected?.Invoke();
        }

        private void OnClientDisconnected()
        {
            OnDisconnected?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MessageBrokerClient));
            
            _isDisposed = true;
            
            OnConnected = null;
            OnDisconnected = null;

            _connectionManager.OnConnected -= OnClientConnected;
            _connectionManager.OnDisconnected -= OnClientDisconnected;

            await _subscriberStore.DisposeAsync();
        }
    }
}