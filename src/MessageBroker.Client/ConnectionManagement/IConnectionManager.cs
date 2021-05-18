using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement.ConnectionStatusEventArgs;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Tcp;
using MessageBroker.Core.Clients;

namespace MessageBroker.Client.ConnectionManagement
{
    /// <summary>
    /// Utility class for managing <see cref="ISocket" /> and <see cref="IClient" />
    /// to provider methods for sending data and connecting, disconnecting and reconnecting to/from server
    /// with auto reconnect feature
    /// </summary>
    public interface IConnectionManager : IDisposable
    {
        /// <summary>
        /// The underlying <see cref="IClient" />
        /// </summary>
        IClient Client { get; }

        /// <summary>
        /// The underlying <see cref="ISocket" />
        /// </summary>
        ISocket Socket { get; }

        /// <summary>
        /// Called when connection is established to broker server
        /// </summary>
        event EventHandler<ClientConnectionEventArgs> OnConnected;

        /// <summary>
        /// Called when connection is disconnected from broker server
        /// </summary>
        event EventHandler<ClientDisconnectedEventArgs> OnDisconnected;

        /// <summary>
        /// Establish connection to server using provided configuration
        /// </summary>
        /// <exception cref="ArgumentNullException">IpEndPoint is null</exception>
        /// <param name="configuration">The configuration for connection</param>
        void Connect(ClientConnectionConfiguration configuration);

        /// <summary>
        /// Will try to reconnect to server if the connection is broker
        /// </summary>
        /// <exception cref="ArgumentNullException">IpEndPoint is null</exception>
        /// <exception cref="InvalidOperationException">Connection is in connected state</exception>
        void Reconnect();

        /// <summary>
        /// Will disconnect the underlying <see cref="ISocket" />
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Will send data to server if the connection is established
        /// otherwise will
        /// </summary>
        /// <param name="serializedPayload"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> SendAsync(SerializedPayload serializedPayload, CancellationToken cancellationToken);
    }
}