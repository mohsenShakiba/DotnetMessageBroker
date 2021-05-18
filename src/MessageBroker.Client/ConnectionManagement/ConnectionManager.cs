using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement.ConnectionStatusEventArgs;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Tcp;
using MessageBroker.Common.Tcp.EventArgs;
using MessageBroker.Core.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client.ConnectionManagement
{
    /// <inheritdoc />
    public class ConnectionManager : IConnectionManager
    {
        private readonly ILogger<ConnectionManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IReceiveDataProcessor _receiveDataProcessor;
        private readonly SemaphoreSlim _semaphore;
        private ClientConnectionConfiguration _configuration;


        public ConnectionManager(IReceiveDataProcessor receiveDataProcessor, ILogger<ConnectionManager> logger,
            IServiceProvider serviceProvider)
        {
            _receiveDataProcessor = receiveDataProcessor;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _semaphore = new SemaphoreSlim(1, 1);
        }


        public IClient Client { get; private set; }
        public ISocket Socket { get; private set; }


        public event EventHandler<ClientConnectionEventArgs> OnConnected;
        public event EventHandler<ClientDisconnectedEventArgs> OnDisconnected;

        public void Connect(ClientConnectionConfiguration configuration)
        {
            _configuration = configuration;

            try
            {
                // wait for semaphore to be release by SendAsync
                // otherwise creating new client while SendAsync is using the old client would cause weired behavior
                _semaphore.Wait();

                // connect the tcp client
                var endPoint = configuration.EndPoint ??
                               throw new ArgumentNullException(nameof(configuration.EndPoint));

                // dispose the old socket and client
                Socket?.Dispose();
                Client?.Dispose();

                // create new tcp socket
                var newTcpSocket = TcpSocket.NewFromEndPoint(endPoint);

                // once the TcpSocket is connected, create new client from it
                var newClient = _serviceProvider.GetRequiredService<IClient>();
                newClient.Setup(newTcpSocket);

                newClient.OnDataReceived += ClientDataReceived;
                newClient.OnDisconnected += ClientDisconnected;

                // start receiving data from server
                newClient.StartReceiveProcess();

                Client = newClient;
                Socket = newTcpSocket;

                _logger.LogInformation(
                    $"Broker client connected to: {_configuration.EndPoint} with auto connect: {_configuration.AutoReconnect}");
            }
            catch (Exception e)
            {
                // if auto reconnect is active, try to reconnect
                if (_configuration.AutoReconnect)
                {
                    _logger.LogWarning($"Couldn't connect to endpoint: {_configuration.EndPoint} with error: {e} retrying in 1 second");
                    
                    Thread.Sleep(1000);
                    
                    Reconnect();
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            // note: must be called after releasing semaphore
            OnConnected?.Invoke(this, new ClientConnectionEventArgs());
        }

        public void Reconnect()
        {
            if (Socket.Connected)
                throw new InvalidOperationException("The socket object is in connected state, cannot be reconnected");

            Connect(_configuration ?? throw new ArgumentNullException("No configuration exists for reconnection"));
        }

        public void Disconnect()
        {
            Socket?.Disconnect();
        }

        public async Task<bool> SendAsync(SerializedPayload serializedPayload, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // wait for connection to be reestablished
                if (!(Socket?.Connected ?? false))
                {
                    _logger.LogTrace("Waiting for broker to reconnect");
                    try
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    catch (TaskCanceledException)
                    {
                        return false;
                    }
                }

                try
                {
                    await _semaphore.WaitAsync(cancellationToken);

                    var result = await Client.SendAsync(serializedPayload.Data, cancellationToken);

                    _logger.LogTrace($"Sending payload with id {serializedPayload.PayloadId}");

                    if (result) return true;

                    if (!_configuration.AutoReconnect) return false;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            // only when cancellation is requested
            return false;
        }

        /// <summary>
        /// Will disconnect and dispose the <see cref="IClient" />
        /// </summary>
        public void Dispose()
        {
            Client?.Dispose();
            Disconnect();
        }

        private void ClientDataReceived(object clientSession, ClientSessionDataReceivedEventArgs eventArgs)
        {
            _receiveDataProcessor.DataReceived(clientSession, eventArgs);
        }

        private void ClientDisconnected(object clientSession, ClientSessionDisconnectedEventArgs eventArgs)
        {
            _logger.LogInformation("Broker client disconnected from server");

            OnDisconnected?.Invoke(this, new ClientDisconnectedEventArgs());

            // check if auto reconnect is enabled
            if (_configuration.AutoReconnect)
            {
                _logger.LogInformation("Trying to reconnect broker client");

                Reconnect();
            }
        }
    }
}