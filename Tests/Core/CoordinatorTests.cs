using System;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Core;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Serialization;
using MessageBroker.TCP.Client;
using Moq;
using Xunit;

namespace Tests.Core
{
    public class CoordinatorTests
    {
        [Fact]
        public void MakeSureWhenSetupIsCalledStoresAreInitialized()
        {
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var sendQueueStore = new Mock<ISendQueueStore>();
            var queueStore = new Mock<IQueueStore>();
            var messageStore = new Mock<IMessageStore>();
            var serializer = new Mock<ISerializer>();

            var coordinator = new Coordinator(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);
            
            coordinator.Setup();

            queueStore.Verify(m => m.Setup());
            messageStore.Verify(m => m.Setup());
        }

        [Fact]
        public void MakeSureWhenDataReceivedIsCalledDataIsPassedToPayloadProcessor()
        {
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var sendQueueStore = new Mock<ISendQueueStore>();
            var queueStore = new Mock<IQueueStore>();
            var messageStore = new Mock<IMessageStore>();
            var serializer = new Mock<ISerializer>();

            var coordinator = new Coordinator(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);
            
            coordinator.DataReceived(It.IsAny<Guid>(), It.IsAny<Memory<byte>>());
            
            payloadProcessor.Verify(p => p.OnDataReceived(It.IsAny<Guid>(), It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void MakeSureWhenClientIsConnectedSendQueueStoreIsNotified()
        {
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var sendQueueStore = new Mock<ISendQueueStore>();
            var queueStore = new Mock<IQueueStore>();
            var messageStore = new Mock<IMessageStore>();
            var serializer = new Mock<ISerializer>();

            var coordinator = new Coordinator(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);

            coordinator.ClientConnected(It.IsAny<IClientSession>());
            
            sendQueueStore.Verify(s => s.Add(It.IsAny<IClientSession>(), It.IsAny<ISendQueue>()));
        }

        [Fact]
        public void MakeSureWhenClientIsDisconnectedSendQueueStoreIsNotified()
        {
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var sendQueueStore = new Mock<ISendQueueStore>();
            var queueStore = new Mock<IQueueStore>();
            var messageStore = new Mock<IMessageStore>();
            var serializer = new Mock<ISerializer>();

            var coordinator = new Coordinator(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);

            coordinator.ClientDisconnected(It.IsAny<IClientSession>());
            
            sendQueueStore.Verify(s => s.Remove(It.IsAny<IClientSession>()));
        }
        
    }
}