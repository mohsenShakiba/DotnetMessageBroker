using System;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Clients.Store;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Topics;
using MessageBroker.TCP;
using MessageBroker.TCP.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MessageBroker.Core
{

    /// <inheritdoc/>
    public class Broker: IBroker
    {
        private readonly IPayloadProcessor _payloadProcessor;
        private readonly IClientStore _clientStore;
        private readonly IMessageStore _messageStore;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Broker> _logger;
        private readonly ITopicStore _topicStore;
        private readonly IListener _listener;
        private bool _disposed;

        public IServiceProvider ServiceProvider => _serviceProvider;

        public Broker(IListener listener, IPayloadProcessor payloadProcessor, IClientStore clientStore, ITopicStore topicStore,
            IMessageStore messageStore, IServiceProvider serviceProvider, ILogger<Broker> logger)
        {
            _listener = listener;
            _payloadProcessor = payloadProcessor;
            _clientStore = clientStore;
            _topicStore = topicStore;
            _messageStore = messageStore;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void Start()
        {
            _listener.OnSocketAccepted += ClientConnected;
            
            _listener.Start();
            _topicStore.Setup();
            _messageStore.Setup();
        }

        public void Stop()
        {
            Dispose();
        }


        private void ClientConnected(object _, SocketAcceptedEventArgs eventArgs)
        {
            try
            {
                var logger = _serviceProvider.GetService<ILogger<Client>>() ?? NullLogger<Client>.Instance;

                var clientSession = new Client(eventArgs.Socket, logger);

                clientSession.OnDisconnected += ClientDisconnected;
                clientSession.OnDataReceived += ClientDataReceived;

                // must add the socket to client store before calling StartReceiveProcess 
                // otherwise we might receive messages before having access to client in client store
                _clientStore.Add(clientSession);

                clientSession.StartReceiveProcess();
                clientSession.StartSendProcess();
            
                _logger.LogInformation($"Client: {clientSession.Id} connected");
   
            }
            catch (ObjectDisposedException)
            {
                // ignore ObjectDisposedException
            }
        }
        
        private void ClientDisconnected(object clientSession, ClientSessionDisconnectedEventArgs eventArgs)
        {

            if (clientSession is IClient client)
            {
                
                _logger.LogInformation($"Client: {client.Id} removed");

                foreach (var queue in _topicStore.GetAll())
                    queue.ClientUnsubscribed(client);

                _clientStore.Remove(client);
            }
        }
        
        private void ClientDataReceived(object clientSession, ClientSessionDataReceivedEventArgs eventArgs)
        {
            try
            {
                _payloadProcessor.OnDataReceived(eventArgs.Id, eventArgs.Data);
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured while trying to dispatch messages, error: {e}");
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _listener.OnSocketAccepted -= ClientConnected;
                _listener.Stop();
                _disposed = true;
            }
   
        }
    }
}