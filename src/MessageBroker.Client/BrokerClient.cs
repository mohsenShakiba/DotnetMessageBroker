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
using MessageBroker.Common.Serialization;

namespace MessageBroker.Client
{
    /// <inheritdoc />
    public class BrokerClient : IBrokerClient
    {
        private readonly IPayloadFactory _payloadFactory;
        private readonly ISendDataProcessor _sendDataProcessor;
        private readonly ISerializer _serializer;
        private readonly ISubscriptionStore _subscriptionStore;
        private readonly ITaskManager _taskManager;

        private bool _isDisposed;

        /// <summary>
        /// Instantiates a new <see cref="BrokerClient" />
        /// </summary>
        /// <remarks>This object is recommended to be created using <see cref="BrokerClientFactory" /></remarks>
        /// <param name="payloadFactory">The <see cref="IPayloadFactory" /></param>
        /// <param name="connectionManager">The <see cref="IConnectionManager" /></param>
        /// <param name="sendDataProcessor">The <see cref="ISendDataProcessor" /></param>
        /// <param name="subscriptionStore">The <see cref="ISubscriptionStore" /></param>
        /// <param name="serializer">The <see cref="ISerializer" /></param>
        /// <param name="taskManager">The <see cref="ITaskManager" /></param>
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

        /// <inheritdoc />
        public bool Connected => ConnectionManager.Socket.Connected;

        /// <inheritdoc />
        public IConnectionManager ConnectionManager { get; }

        /// <inheritdoc />
        public void Connect(ClientConnectionConfiguration configuration)
        {
            ConnectionManager.Connect(configuration);
        }

        /// <inheritdoc />
        public void Reconnect()
        {
            ConnectionManager.Reconnect();
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            ConnectionManager.Disconnect();
        }

        /// <inheritdoc />
        public async Task<ISubscription> GetTopicSubscriptionAsync(string name,
            CancellationToken? cancellationToken = null)
        {
            var subscription = new Subscription(_payloadFactory, ConnectionManager, _sendDataProcessor);

            _subscriptionStore.Add(name, subscription);

            await subscription.SetupAsync(name, cancellationToken ?? CancellationToken.None);

            return subscription;
        }

        /// <inheritdoc />
        public Task<SendAsyncResult> PublishAsync(string route, byte[] data,
            CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewMessage(data, route);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<SendAsyncResult> DeclareTopicAsync(string name, string route,
            CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewDeclareTopic(name, route);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<SendAsyncResult> DeleteTopicAsync(string name, CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewDeleteTopic(name);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<SendAsyncResult> ConfigureClientAsync(int prefetchCount,
            CancellationToken? cancellationToken = null)
        {
            var serializedPayload = _payloadFactory.NewConfigureClient(prefetchCount);
            return _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken ?? CancellationToken.None);
        }

        /// <inheritdoc />
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