using System;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.Subscription;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Client
{
    public class MessageBrokerClient
    {
        private readonly IConnectionManager _connectionManager;
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


        public void Connect(IPEndPoint ipEndPoint)
        {
            _connectionManager.Connect(ipEndPoint);
        }

        public void Disconnect()
        {
            _connectionManager.Disconnect();
        }

        public ISubscriber GetQueueSubscriber(string name, string route)
        {
            var queueManager = _serviceProvider.GetRequiredService<ISubscriber>();
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

        public QueueSubscription SubscribeAsync(string queueName)
        {
            return new 
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