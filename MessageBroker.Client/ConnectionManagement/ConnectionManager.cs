using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement.ConnectionStatusEventArgs;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Clients;
using MessageBroker.TCP;
using MessageBroker.TCP.EventArgs;
using MessageBroker.TCP.Binary;

namespace MessageBroker.Client.ConnectionManagement
{
    /// <inheritdoc />
    public class ConnectionManager : IConnectionManager
    {
        private readonly IReceiveDataProcessor _receiveDataProcessor;

        private ClientConnectionConfiguration _configuration;
        private ConcurrentQueue<Guid> _ids = new();
        private SemaphoreSlim _semaphore;
        private bool _debug;


        public IClient Client { get; private set; }
        public ITcpSocket Socket { get; private set; }
        public IReceiveDataProcessor ReceiveDataProcessor => _receiveDataProcessor;


        public event EventHandler<ClientConnectionEventArgs> OnConnected;
        public event EventHandler<ClientDisconnectedEventArgs> OnDisconnected;


        public ConnectionManager(IReceiveDataProcessor receiveDataProcessor)
        {
            _receiveDataProcessor = receiveDataProcessor;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public void Connect(ClientConnectionConfiguration configuration, bool debug)
        {
            _debug = debug;

            // todo: remove
            try
            {
                // Client?.Dispose();

                _configuration = configuration;

   

                // once the TcpSocket is connected, create new client from it

                _semaphore.Wait();
                
                // connect the tcp client
                var ipEndpoint = configuration.IpEndPoint ??
                                 throw new ArgumentNullException(nameof(configuration.IpEndPoint));
                
                var newTcpSocket = TcpSocket.NewFromEndPoint(ipEndpoint);
                
                var newClient = new Core.Clients.Client(newTcpSocket);

                newClient.OnDataReceived += ClientDataReceived;
                newClient.OnDisconnected += ClientDisconnected;

                newClient.Debug = debug;
                newClient.StartReceiveProcess();

                Client = newClient;
                Socket = newTcpSocket;
                
                Logger.LogInformation("client socket connected");
                
                _semaphore.Release();


                OnConnected?.Invoke(this, new ClientConnectionEventArgs());


                // no need to call StartSendProcess because we are not using Enqueue method
                // Client.StartSendProcess();\
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Reconnect()
        {
            if (Socket.Connected)
            {
                throw new InvalidOperationException("The socket object is in connected state, cannot be reconnected");
            }

            Connect(_configuration ?? throw new ArgumentNullException($"No configuration exists for reconnection"),
                _debug);
        }

        public void Disconnect()
        {
            Socket?.Disconnect();
        }

        public async Task<bool> SendAsync(SerializedPayload serializedPayload, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                
                if (!Socket.Connected)
                {
                    Logger.LogInformation("client socket not connected waiting for connection to be established");
                    await Task.Delay(10);
                    continue;
                }
                
                _semaphore.Wait();

                try
                {

                    var result = await Client.SendAsync(serializedPayload.Data, cancellationToken);
                    
                    Logger.LogInformation($"Sending payload with id {serializedPayload.PayloadId} and result {result}");

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
            try
            {
                _receiveDataProcessor.DataReceived(clientSession, eventArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ClientDisconnected(object clientSession, ClientSessionDisconnectedEventArgs eventArgs)
        {
            Logger.LogInformation("received event for socket disconnected");
            _ids.Enqueue(((IClient) clientSession).Id);

            OnDisconnected?.Invoke(this, new ClientDisconnectedEventArgs());

            // check if auto reconnect is enabled
            if (_configuration.AutoReconnect)
            {
                Logger.LogInformation("auto connect in true, trying to reconnect");
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