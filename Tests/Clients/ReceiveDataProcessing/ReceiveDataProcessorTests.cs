using System;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Payloads;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.SendDataProcessing;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Client.Subscriptions.Store;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Models;
using MessageBroker.Common.Serialization;
using MessageBroker.Common.Tcp.EventArgs;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Clients.ReceiveDataProcessing
{
    public class ReceiveDataProcessorTests
    {
        [Fact]
        public void DataReceived_DataIsMessage_DispatchToSubscription()
        {
            var deserializer = new Deserializer();
            var subscriptionStore = new Mock<ISubscriptionStore>();
            var taskManager = new Mock<ITaskManager>();
            var connectionManager = new Mock<IConnectionManager>();
            var sendDataProcessor = new Mock<ISendDataProcessor>();
            var payloadFactory = new Mock<IPayloadFactory>();
            var subscription = new Mock<Subscription>(payloadFactory.Object, connectionManager.Object, sendDataProcessor.Object);
            var subscriptionObject = subscription.Object as ISubscription;

            subscriptionStore.Setup(s => s.TryGet(It.IsAny<string>(), out subscriptionObject)).Returns(true);

            var receiveDataProcessor = new ReceiveDataProcessor(deserializer,
                subscriptionStore.Object,
                taskManager.Object);

            var message = new TopicMessage()
            {
                Id = Guid.NewGuid(),
                Route = RandomGenerator.GenerateString(10),
                TopicName = RandomGenerator.GenerateString(10),
                Data = RandomGenerator.GenerateBytes(10)
            };
            
            var realSerializer = new Serializer();
            var serializedPayload = realSerializer.Serialize(message);

            var clientSessionDataReceivedEventArgs = new ClientSessionDataReceivedEventArgs
            {
                Data = serializedPayload.DataWithoutSize,
                Id = Guid.NewGuid()
            };
            
            receiveDataProcessor.DataReceived(default, clientSessionDataReceivedEventArgs);
            
            subscription.Verify(s => s.OnMessageReceived(It.IsAny<TopicMessage>()));
        }

        [Fact]
        public void DataReceived_DataIsOk_TaskManagerIsCalled()
        {
            var deserializer = new Deserializer();
            var subscriptionStore = new Mock<ISubscriptionStore>();
            var taskManager = new Mock<ITaskManager>();

            var receiveDataProcessor = new ReceiveDataProcessor(deserializer,
                            subscriptionStore.Object,
                            taskManager.Object);
            
            var ok = new Ok()
            {
                Id = Guid.NewGuid(),
            };
            
            var realSerializer = new Serializer();
            var serializedPayload = realSerializer.Serialize(ok);
            
            var clientSessionDataReceivedEventArgs = new ClientSessionDataReceivedEventArgs
            {
                Data = serializedPayload.DataWithoutSize,
                Id = Guid.NewGuid()
            };
            
            receiveDataProcessor.DataReceived(default, clientSessionDataReceivedEventArgs);
    
            taskManager.Verify(stm => stm.OnPayloadOkResult(It.IsAny<Guid>()));
        }
        
        [Fact]
        public void DataReceived_DataIsError_TaskManagerIsCalled()
        {
            var deserializer = new Deserializer();
            var subscriptionStore = new Mock<ISubscriptionStore>();
            var taskManager = new Mock<ITaskManager>();

            var receiveDataProcessor = new ReceiveDataProcessor(deserializer,
                subscriptionStore.Object,
                taskManager.Object);
            
            var error = new Error
            {
                Id = Guid.NewGuid(),
                Message = RandomGenerator.GenerateString(10)
            };
            
            var realSerializer = new Serializer();
            var serializedPayload = realSerializer.Serialize(error);
            
            var clientSessionDataReceivedEventArgs = new ClientSessionDataReceivedEventArgs
            {
                Data = serializedPayload.DataWithoutSize,
                Id = Guid.NewGuid()
            };
            
            receiveDataProcessor.DataReceived(default, clientSessionDataReceivedEventArgs);
    
            taskManager.Verify(stm => stm.OnPayloadErrorResult(It.IsAny<Guid>(), It.IsAny<string>()));
        }
    }
}

