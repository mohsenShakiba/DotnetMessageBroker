using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement.ConnectionStatusEventArgs;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Core.Clients;
using MessageBroker.Models.Binary;
using MessageBroker.TCP;
using MessageBroker.TCP.EventArgs;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client.ConnectionManagement
{
    /// <inheritdoc />
    public class ConnectionManager : IConnectionManager
    {
        private readonly IReceiveDataProcessor _receiveDataProcessor;
        private readonly ILogger<ConnectionManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private ClientConnectionConfiguration _configuration;
        private SemaphoreSlim _semaphore;


        public IClient Client { get; private set; }
        public ISocket Socket { get; private set; }


        public event EventHandler<ClientConnectionEventArgs> OnConnected;
        public event EventHandler<ClientDisconnectedEventArgs> OnDisconnected;


        public ConnectionManager(IReceiveDataProcessor receiveDataProcessor, ILogger<ConnectionManager> logger, ILoggerFactory loggerFactory)
        {
            _receiveDataProcessor = receiveDataProcessor;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public void Connect(ClientConnectionConfiguration configuration)
        {
            _configuration = configuration;

            try
            {

                // wait for semphore to be release by SendAsync
                // otherwise creating new client while SendAsync is using the old client would cause weired behavior
                _semaphore.Wait();

                // connect the tcp client
                var ipEndpoint = configuration.IpEndPoint ??
                                 throw new ArgumentNullException(nameof(configuration.IpEndPoint));

                // dispose the old socket and client
                Socket?.Dispose();
                Client?.Dispose();

                // create new tcp socket
                var newTcpSocket = TCP.TcpSocket.NewFromEndPoint(ipEndpoint);

                // once the TcpSocket is connected, create new client from it
                var logger = _loggerFactory.CreateLogger<Core.Clients.Client>();
                var newClient = new Core.Clients.Client(newTcpSocket, logger);

                newClient.OnDataReceived += ClientDataReceived;
                newClient.OnDisconnected += ClientDisconnected;

                // start receiving data from server
                newClient.StartReceiveProcess();

                Client = newClient;
                Socket = newTcpSocket;

                _logger.LogInformation($"Broker client connected to: {_configuration.IpEndPoint} with auto connect: {_configuration.AutoReconnect}");
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
            {
                throw new InvalidOperationException("The socket object is in connected state, cannot be reconnected");
            }

            Connect(_configuration ?? throw new ArgumentNullException($"No configuration exists for reconnection"));
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
                if (!Socket.Connected)
                {
                    _logger.LogTrace("Waiting for broker to reconnect");
                    await Task.Delay(10);
                    continue;
                }
                
                _semaphore.Wait();

                try
                {

                    var result = await Client.SendAsync(serializedPayload.Data, cancellationToken);
                    
                    _logger.LogTrace($"Sending payload with id {serializedPayload.PayloadId}");

                    // if success then exit loop and return true
                    if (result)
                    {
                        return true;
                    }

                    // if auto connect is enabled, wait for socket to be re-established
                    if (_configuration.AutoReconnect)
                    {
                        await Task.Delay(5);
                    }
                    // otherwise break and return false
                    else
                    {
                        return false;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
       
            }

            // only when cancellation is requested
            return false;
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

        /// <summary>
        /// Will disconnect and dispose the <see cref="IClient"/>
        /// </summary>
        public void Dispose()
        {
            Client?.Dispose();
            Disconnect();
        }
    }
}