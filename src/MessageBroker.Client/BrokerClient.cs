using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.Payloads;
using MessageBroker.Client.SendDataProcessing;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Client.Subscriptions.Store;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Models;
using MessageBroker.Common.Serialization;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client
{
    public class BrokerClient : IBrokerClient
    {
        private readonly IPayloadFactory _payloadFactory;
        private readonly ISendDataProcessor _sendDataProcessor;
        private readonly ITaskManager _taskManager;
        private readonly ISerializer _serializer;
        private readonly ISubscriptionStore _subscriptionStore;

        private bool _isDisposed;

        public BrokerClient(IPayloadFactory payloadFactory, IConnectionManager connectionManager,
            ISendDataProcessor sendDataProcessor, ISubscriptionStore subscriptionStore, ISerializer serializer,
            ITaskManager taskManager)
        {
            _payloadFactory = payloadFactory;
            ConnectionManager = connectionManager;
            _subscriptionStore = subscriptionStore;
            _sendDataProcessor = sendDataProcessor;
            _serializer = serializer;
            _taskManager = taskManager;
        }

        public bool Connected => ConnectionManager.Socket.Connected;
        public IConnectionManager ConnectionManager { get; }

        public void Connect(ClientConnectionConfiguration configuration)
        {
            ConnectionManager.Connect(configuration);
        }

        public void Reconnect()
        {
            ConnectionManager.Reconnect();
        }

        public void Disconnect()
        {
            ConnectionManager.Disconnect();
        }
        
        public async Task<ISubscription> GetTopicSubscriptionAsync(string name, string route,
            CancellationToken? cancellationToken = null)
        {
            var subscription = new Subscription(_payloadFactory, ConnectionManager, _sendDataProcessor);

            _subscriptionStore.Add(name, subscription);

            await subscription.SetupAsync(name, route, cancellationToken ?? CancellationToken.None);

            return subscription;
        }

        public Task<SendAsyncResult> PublishAsync(string route, byte[] data,
            CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewMessage(data, route);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        public Task<SendAsyncResult> PublishRawAsync(Message message, bool waitForAcknowledge,
            CancellationToken cancellationToken)
        {
            var serializedPayload = _serializer.Serialize(message);
            return _sendDataProcessor.SendAsync(serializedPayload, waitForAcknowledge, cancellationToken);
        }

        public Task<SendAsyncResult> DeclareTopicAsync(string name, string route,
            CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewDeclareTopic(name, route);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        public Task<SendAsyncResult> DeleteTopicAsync(string name, CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewDeleteTopic(name);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        public Task<SendAsyncResult> ConfigureClientAsync(int prefetchCount, CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewConfigureClient(prefetchCount);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(BrokerClient));

            _isDisposed = true;

            await _subscriptionStore.DisposeAsync();

            _taskManager.Dispose();
        }
    }
}