using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Common.Models;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client
{
    public interface IBrokerClient : IAsyncDisposable
    {
        public bool Connected { get; }
        public IConnectionManager ConnectionManager { get; }

        void Connect(ClientConnectionConfiguration configuration);
        void Reconnect();
        void Disconnect();

        Task<ISubscription> GetTopicSubscriptionAsync(string name, string route,
            CancellationToken? cancellationToken = null);

        Task<SendAsyncResult> PublishAsync(string route, byte[] data, CancellationToken? cancellationToken = null);

        Task<SendAsyncResult> PublishRawAsync(Message message, bool waitForAcknowledge,
            CancellationToken cancellationToken);

        Task<SendAsyncResult> DeclareTopicAsync(string name, string route, CancellationToken? cancellationToken = null);
        Task<SendAsyncResult> DeleteTopicAsync(string name, CancellationToken? cancellationToken = null);
        Task<SendAsyncResult> ConfigureClientAsync(int prefetchCount, CancellationToken? cancellationToken = null);
    }
}