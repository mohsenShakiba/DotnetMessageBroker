using System;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManager;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueManagement;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Client
{
    public class MessageBrokerClient
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IReceiveDataProcessor _eceiveDataProcessor;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly ISerializer _serializer;
        private readonly IServiceProvider _serviceProvider;


        public MessageBrokerClient(ISerializer serializer, ISendPayloadTaskManager sendPayloadTaskManager,
            IConnectionManager connectionManager, IServiceProvider serviceProvider)
        {
            _serializer = serializer;
            _sendPayloadTaskManager = sendPayloadTaskManager;
            _connectionManager = connectionManager;
            _serviceProvider = serviceProvider;
        }


        public void Connect(SocketConnectionConfiguration configuration)
        {
            _connectionManager.Connect(configuration);
        }

        public void Disconnect()
        {
            _connectionManager.Disconnect();
        }

        public IQueueManager GetQueueConsumer(string name, string route)
        {
            var queueManager = _serviceProvider.GetRequiredService<IQueueManager>();
            queueManager.Setup(name, route);
            return queueManager;
        }


        public Task<SendAsyncResult> PublishAsync(string route, byte[] data, bool completedOnAcknowledge = true)
        {
            var sendPayload = GetMessageData(route, data);
            return SendAsync(sendPayload, completedOnAcknowledge);
        }

        public Task<SendAsyncResult> AckAsync(Guid messageId)
        {
            var sendPayload = GetAckData(messageId);
            return SendAsync(sendPayload, false);
        }

        public Task<SendAsyncResult> NackAsync(Guid messageId)
        {
            var sendPayload = GetNackData(messageId);
            return SendAsync(sendPayload, false);
        }

        public Task<SendAsyncResult> ConfigureClientAsync(int concurrency, bool autoAck)
        {
            var sendPayload = GetConfigureSubscriptionData(concurrency, autoAck);
            return SendAsync(sendPayload, true);
        }

        private async Task<SendAsyncResult> SendAsync(SerializedPayload serializedPayload, bool completeOnAcknowledge)
        {
            var clientSession = _connectionManager.ClientSession;

            var sendPayloadTask = _sendPayloadTaskManager.Setup(serializedPayload.Id, completeOnAcknowledge);

            var sendSuccess = await clientSession.SendAsync(serializedPayload.Data);

            if (sendSuccess)
                _sendPayloadTaskManager.OnPayloadSendSuccess(serializedPayload.Id);
            else
                _sendPayloadTaskManager.OnPayloadSendFailed(serializedPayload.Id);

            return await sendPayloadTask;
        }

        private SerializedPayload GetMessageData(string route, byte[] data)
        {
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                Data = data,
                Route = route
            };

            return _serializer.Serialize(msg);
        }

        private SerializedPayload GetAckData(Guid messageId)
        {
            var msg = new Ack
            {
                Id = messageId
            };

            return _serializer.Serialize(msg);
        }

        private SerializedPayload GetNackData(Guid messageId)
        {
            var msg = new Nack
            {
                Id = messageId
            };

            return _serializer.Serialize(msg);
        }

        private SerializedPayload GetConfigureSubscriptionData(int concurrency, bool autoAck)
        {
            var msg = new ConfigureSubscription
            {
                Id = Guid.NewGuid(),
                Concurrency = concurrency,
                AutoAck = autoAck
            };

            return _serializer.Serialize(msg);
        }
    }
}