using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.Payloads;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.SendDataProcessing;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Client
{
    public class BrokerClient : IBrokerClient
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ISubscriptionStore _subscriptionStore;
        private readonly ISendDataProcessor _sendDataProcessor;
        private readonly ISerializer _serializer;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly IPayloadFactory _payloadFactory;

        private bool _isDisposed;

        public bool Connected => _connectionManager.Socket.Connected;
        public IConnectionManager ConnectionManager => _connectionManager;

        public BrokerClient(IPayloadFactory payloadFactory, IConnectionManager connectionManager,
            ISendDataProcessor sendDataProcessor, ISubscriptionStore subscriptionStore, ISerializer serializer, ISendPayloadTaskManager sendPayloadTaskManager)
        {
            _payloadFactory = payloadFactory;
            _connectionManager = connectionManager;
            _subscriptionStore = subscriptionStore;
            _sendDataProcessor = sendDataProcessor;
            _serializer = serializer;
            _sendPayloadTaskManager = sendPayloadTaskManager;
        }

        public void Connect(ClientConnectionConfiguration configuration, bool debug)
        {
            _connectionManager.Connect(configuration, debug);
        }

        public void Reconnect()
        {
            _connectionManager.Reconnect();
        }

        public void Disconnect()
        {
            _connectionManager.Disconnect();
        }

        public async Task<ISubscription> GetTopicSubscriptionAsync(string name, string route,
            CancellationToken? cancellationToken = null)
        {
            var subscription = new Subscription(_payloadFactory, _connectionManager, _sendDataProcessor);

            _subscriptionStore.Add(name, subscription);

            await subscription.SetupAsync(name, route, cancellationToken ?? CancellationToken.None);

            return subscription;
        }

        public Task<SendAsyncResult> PublishAsync(string route, byte[] data,
            CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewMessage(data, route);
            return _sendDataProcessor.SendAsync(serializedPayload, false, cancellationToken ?? CancellationToken.None);
        }

        public Task<SendAsyncResult> PublishRawAsync(Message message, CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _serializer.Serialize(message);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        public Task<SendAsyncResult> DeclareTopicAsync(string queueName, string queueRoute,
            CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewDeclareTopic(queueName, queueRoute);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        public Task<SendAsyncResult> DeleteTopicAsync(string queueName, CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewDeleteTopic(queueName);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(BrokerClient));

            _isDisposed = true;

            await _subscriptionStore.DisposeAsync();
            _sendPayloadTaskManager.Dispose();
        }
    }
}