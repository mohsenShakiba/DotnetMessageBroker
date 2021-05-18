using System;
using MessageBroker.Common.Tcp;
using MessageBroker.Common.Tcp.EventArgs;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Clients.Store;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MessageBroker.Core
{
    /// <inheritdoc />
    public class Broker : IBroker
    {
        private readonly IClientStore _clientStore;
        private readonly IListener _listener;
        private readonly ILogger<Broker> _logger;
        private readonly IMessageStore _messageStore;
        private readonly IPayloadProcessor _payloadProcessor;
        private readonly ITopicStore _topicStore;
        private bool _disposed;

        public Broker(IListener listener, IPayloadProcessor payloadProcessor, IClientStore clientStore,
            ITopicStore topicStore,
            IMessageStore messageStore, IServiceProvider serviceProvider, ILogger<Broker> logger)
        {
            _listener = listener;
            _payloadProcessor = payloadProcessor;
            _clientStore = clientStore;
            _topicStore = topicStore;
            _messageStore = messageStore;
            _logger = logger;
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

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

        public void Dispose()
        {
            if (!_disposed)
            {
                _listener.OnSocketAccepted -= ClientConnected;
                _listener.Stop();
                _disposed = true;
            }
        }


        private void ClientConnected(object _, SocketAcceptedEventArgs eventArgs)
        {
            try
            {
                var clientSession = ServiceProvider.GetRequiredService<IClient>();
                
                clientSession.Setup(eventArgs.Socket);

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
    }
}