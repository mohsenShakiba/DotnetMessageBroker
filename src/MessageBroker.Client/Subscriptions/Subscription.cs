using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.ConnectionManagement.ConnectionStatusEventArgs;
using MessageBroker.Client.Models;
using MessageBroker.Client.Payloads;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.SendDataProcessing;
using MessageBroker.Common.Models;

namespace MessageBroker.Client.Subscriptions
{
    /// <inheritdoc />
    public class Subscription : ISubscription
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IPayloadFactory _payloadFactory;
        private readonly ISendDataProcessor _sendDataProcessor;

        private bool _disposed;


        public Subscription(IPayloadFactory payloadFactory, IConnectionManager connectionManager,
            ISendDataProcessor sendDataProcessor)
        {
            _payloadFactory = payloadFactory;
            _connectionManager = connectionManager;
            _sendDataProcessor = sendDataProcessor;

            connectionManager.OnConnected += OnConnected;
        }

        public event Action<SubscriptionMessage> MessageReceived;
        public string Name { get; private set; }
        public string Route { get; private set; }

        public async ValueTask DisposeAsync()
        {
            _disposed = true;

            MessageReceived = null;

            _connectionManager.OnConnected -= OnConnected;

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

            await UnSubscribeAsync(cancellationTokenSource.Token);
        }


        public async Task SetupAsync(string name, string route, CancellationToken cancellationToken)
        {
            Name = name;
            Route = route;

            await SubscribeAsync(cancellationToken);
        }

        /// <summary>
        /// This method is used by <see cref="IReceiveDataProcessor"/> to dispatch received messages 
        /// </summary>
        /// <param name="topicMessage">Message received from server</param>
        public virtual void OnMessageReceived(TopicMessage topicMessage)
        {
            try
            {
                ThrowIfDisposed();

                var subscriptionMessage = new SubscriptionMessage
                {
                    MessageId = topicMessage.Id,
                    Data = topicMessage.Data,
                    TopicRoute = topicMessage.Route,
                    TopicName = topicMessage.TopicName
                };

                subscriptionMessage.OnMessageProcessedByClient += OnMessageProcessedByClient;

                MessageReceived?.Invoke(subscriptionMessage);
            }
            // if message process failed then mark it as nacked
            catch
            {
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                OnMessageProcessedByClient(topicMessage.Id, false, cancellationTokenSource.Token);
            }
        }

        private async void OnMessageProcessedByClient(Guid messageId, bool ack, CancellationToken cancellationToken)
        {
            if (ack)
                await AckAsync(messageId, cancellationToken);
            else
                await NackAsync(messageId, cancellationToken);
        }

        private async Task SubscribeAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var serializedPayload = _payloadFactory.NewSubscribeTopic(Name);

            var result = await _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken);

            if (!result.IsSuccess)
                throw new Exception($"Failed to create subscription, error: {result.InternalErrorCode}");
        }

        private async Task UnSubscribeAsync(CancellationToken cancellationToken)
        {
            var serializedPayload = _payloadFactory.NewUnsubscribeTopic(Name);

            try
            {
                await _sendDataProcessor.SendAsync(serializedPayload, true, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // ignore ObjectDisposedException
            }
        }

        private async Task AckAsync(Guid messageId, CancellationToken cancellationToken)
        {
            var serializedPayload = _payloadFactory.NewAck(messageId);
            await _sendDataProcessor.SendAsync(serializedPayload, false, cancellationToken);
        }

        private async Task NackAsync(Guid messageId, CancellationToken cancellationToken)
        {
            var serializedPayload = _payloadFactory.NewNack(messageId);
            await _sendDataProcessor.SendAsync(serializedPayload, false, cancellationToken);
        }


        private async void OnConnected(object connectionManager, ClientConnectionEventArgs e)
        {
            await SubscribeAsync(CancellationToken.None);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"{nameof(Subscription)} is disposed and cannot be accessed");
        }
    }
}