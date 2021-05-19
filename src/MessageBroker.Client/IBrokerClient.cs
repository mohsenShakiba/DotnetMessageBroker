using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.Subscriptions;

namespace MessageBroker.Client
{
    /// <summary>
    /// Client that is used to connect to server in order to perform various tasks such as managing topics and
    /// sending messages
    /// </summary>
    public interface IBrokerClient : IAsyncDisposable
    {
        /// <summary>
        /// Returns true if the socket is connected, otherwise false
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// Returns the underlying <see cref="IConnectionManager" />
        /// </summary>
        public IConnectionManager ConnectionManager { get; }

        /// <summary>
        /// Connects to server using the <see cref="ClientConnectionConfiguration" />
        /// </summary>
        /// <param name="configuration"></param>
        void Connect(ClientConnectionConfiguration configuration);

        /// <summary>
        /// Reconnects to server using the previously provided <see cref="ClientConnectionConfiguration" />
        /// </summary>
        void Reconnect();

        /// <summary>
        /// Disconnects from server if connected
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Creates a new <see cref="ISubscription" /> from the provided topic name that can received messages
        /// sent to a topic
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <param name="cancellationToken"><see cref="CancellationToken" /> used for async operations</param>
        /// <returns><see cref="SendAsyncResult" /> containing the response</returns>
        Task<ISubscription> GetTopicSubscriptionAsync(string name, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Publish message to server using the provided data and topic route
        /// </summary>
        /// <param name="route">Route of topic</param>
        /// <param name="data">Message data</param>
        /// <param name="cancellationToken">CancellationToken used for async operations</param>
        /// <returns><see cref="SendAsyncResult" /> containing the response</returns>
        Task<SendAsyncResult> PublishAsync(string route, byte[] data, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Will send a request to create topic with the provided name and route
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <param name="route">Route of topic</param>
        /// <param name="cancellationToken">CancellationToken used for async operations</param>
        /// <returns><see cref="SendAsyncResult" /> containing the response</returns>
        Task<SendAsyncResult> DeclareTopicAsync(string name, string route, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Will send a request to delete topic with the provided name
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <param name="cancellationToken">CancellationToken used for async operations</param>
        /// <returns><see cref="SendAsyncResult" /> containing the response</returns>
        Task<SendAsyncResult> DeleteTopicAsync(string name, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Will send a request to server configuring the prefetch count of client
        /// </summary>
        /// <param name="prefetchCount">Prefetch count of client</param>
        /// <param name="cancellationToken">CancellationToken used for async operations</param>
        /// <returns><see cref="SendAsyncResult" /> containing the response</returns>
        Task<SendAsyncResult> ConfigureClientAsync(int prefetchCount, CancellationToken? cancellationToken = null);
    }
}