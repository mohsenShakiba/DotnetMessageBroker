using System;
using System.Collections.Generic;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Clients.Store;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Topics;
using MessageBroker.Core.Stats.TopicStatus;
using MessageBroker.Models;
using MessageBroker.Serialization;
using MessageBroker.TCP;
using MessageBroker.TCP.EventArgs;

namespace MessageBroker.Core.Broker
{
    /// <summary>
    /// Instance of message broker
    /// </summary>
    public class Broker: IBroker
    {
        private readonly IPayloadProcessor _payloadProcessor;
        private readonly IClientStore _clientStore;
        private readonly IMessageStore _messageStore;
        private readonly ISerializer _serializer;
        private readonly ITopicStore _topicStore;
        private readonly ISocketServer _socketServer;

        public Broker(ISocketServer socketServer, IPayloadProcessor payloadProcessor, IClientStore clientStore, ITopicStore topicStore,
            IMessageStore messageStore, ISerializer serializer)
        {
            _socketServer = socketServer;
            _payloadProcessor = payloadProcessor;
            _clientStore = clientStore;
            _topicStore = topicStore;
            _messageStore = messageStore;
            _serializer = serializer;
        }

        public void Start()
        {
            _socketServer.OnSocketAccepted += ClientConnected;
            
            _socketServer.Start();
            _topicStore.Setup();
            _messageStore.Setup();
        }

        public void Stop()
        {
            Dispose();
        }

        public ITopicStatRecorder GetStatRecorderForTopic(string topicName)
        {
            if (_topicStore.TryGetValue(topicName, out var topic))
            {
                return topic.StatRecorder;
            }

            throw new KeyNotFoundException("No topic with provided key was found");
        }

        private void ClientConnected(object _, SocketAcceptedEventArgs eventArgs)
        {
            try
            {
                var clientSession = new Client(eventArgs.Socket);

                clientSession.OnDisconnected += ClientDisconnected;
                clientSession.OnDataReceived += ClientDataReceived;
            
                clientSession.StartReceiveProcess();
                clientSession.StartSendProcess();
            
                Logger.LogInformation($"client session connected, added send queue {clientSession.Id}");
   
                _clientStore.Add(clientSession);
            }
            catch (ObjectDisposedException)
            {
                // ignore ObjectDisposedException
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
                Logger.LogError($"An error occured while trying to dispatch messages, error: {e}");
            }
        }

        private void ClientDisconnected(object clientSession, ClientSessionDisconnectedEventArgs eventArgs)
        {
            Logger.LogInformation("client session disconnected, removing send queue");

            if (clientSession is IClient client)
            {
                
                foreach (var queue in _topicStore.GetAll())
                    queue.ClientUnsubscribed(client);
                
                client.OnDisconnected -= ClientDisconnected;
                client.OnDataReceived -= ClientDataReceived;
                _clientStore.Remove(client);
            }
        }
        
        public void Dispose()
        {
            _socketServer.OnSocketAccepted -= ClientConnected;

            _socketServer.Stop();

        }
    }
}