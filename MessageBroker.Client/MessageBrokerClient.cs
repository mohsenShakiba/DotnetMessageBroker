using System;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManager;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.SocketClient;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MessageBroker.Client
{
    public class MessageBrokerClient
    {
        private readonly ISerializer _serializer;
        private readonly ITaskManager _taskManager;
        private readonly IConnectionManager _connectionManager;
        private readonly IReceiveDataProcessor _receiveDataProcessor;
        private readonly IQueueConsumerCoordinator _queueConsumerCoordinator;
        private readonly IBinaryDataProcessor _binaryDataProcessor;
        
        private ISocketClient _socketClient;
        private ILoggerFactory _loggerFactory;

        public MessageBrokerClient()
        {
            _serializer = new Serializer();
            _taskManager = new DefaultTaskManager();
            _queueConsumerCoordinator = new QueueConsumerCoordinator();
            _connectionManager =
                new ConnectionManager.ConnectionManager(NullLogger<ConnectionManager.ConnectionManager>.Instance);
            _loggerFactory = NullLoggerFactory.Instance;
            _binaryDataProcessor = new BinaryDataProcessor();
            _receiveDataProcessor = new ReceiveDataProcessor(_binaryDataProcessor, _serializer, _queueConsumerCoordinator, _taskManager);
            _socketClient = new SocketClient.SocketClient(_loggerFactory, _taskManager, _receiveDataProcessor, _connectionManager);
        }


        public void Connect(SocketConnectionConfiguration configuration)
        {
            _socketClient.Connect(configuration);
        }

        public void Disconnect()
        {
            _connectionManager.Disconnect();
        }

        public QueueManager GetQueueConsumer(string name, string route)
        {
            var queueManager = new QueueManager(_serializer, _socketClient, _queueConsumerCoordinator);
            queueManager.Setup(name, route);
            return queueManager;
        }


        public Task<SendAsyncResult> PublishAsync(string route, byte[] data)
        {
            var sendPayload = GetMessageData(route, data);
            return _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, true);
        }

        public Task<SendAsyncResult> AckAsync(Guid messageId)
        {
            var sendPayload = GetAckData(messageId);
            return _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, false);
        }

        public Task<SendAsyncResult> NackAsync(Guid messageId)
        {
            var sendPayload = GetNackData(messageId);
            return _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, false);
        }

        public Task<SendAsyncResult> ConfigureClientAsync(int concurrency, bool autoAck)
        {
            var sendPayload = GetConfigureSubscriptionData(concurrency, autoAck);
            return _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, false);
        }


        private SerializedPayload GetMessageData(string route, byte[] data)
        {
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                Data = data,
                Route = route,
            };

            return _serializer.Serialize(msg);
        }
        
        private SerializedPayload GetAckData(Guid messageId)
        {
            var msg = new Ack
            {
                Id = messageId,
            };

            return _serializer.Serialize(msg);
        }
        
        private SerializedPayload GetNackData(Guid messageId)
        {
            var msg = new Nack
            {
                Id = messageId,
            };

            return _serializer.Serialize(msg);
        }
        
        private SerializedPayload GetConfigureSubscriptionData(int concurrency, bool autoAck)
        {
            var msg = new ConfigureSubscription()
            {
                Id = Guid.NewGuid(),
                Concurrency = concurrency,
                AutoAck = autoAck
            };

            return _serializer.Serialize(msg);
        }

        public void SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
    }
}