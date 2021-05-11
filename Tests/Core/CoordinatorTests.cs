// using System;
// using MessageBroker.Client.ConnectionManagement;
// using MessageBroker.Core;
// using MessageBroker.Core.Broker;
// using MessageBroker.Core.PayloadProcessing;
// using MessageBroker.Core.Persistence.Messages;
// using MessageBroker.Core.Persistence.Topics;
// using MessageBroker.Core.Queues;
// using MessageBroker.Core.Queues.Store;
// using MessageBroker.Serialization;
// using MessageBroker.TCP.Client;
// using Moq;
// using Xunit;
//
// namespace Tests.Core
// {
//     public class CoordinatorTests
//     {
//         [Fact]
//         public void MakeSureWhenSetupIsCalledStoresAreInitialized()
//         {
//             var payloadProcessor = new Mock<IPayloadProcessor>();
//             var sendQueueStore = new Mock<IClientStore>();
//             var queueStore = new Mock<ITopicStore>();
//             var messageStore = new Mock<IMessageStore>();
//             var serializer = new Mock<ISerializer>();
//
//             var coordinator = new Broker(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);
//             
//             coordinator.Setup();
//
//             queueStore.Verify(m => m.Setup());
//             messageStore.Verify(m => m.Setup());
//         }
//
//         [Fact]
//         public void MakeSureWhenDataReceivedIsCalledDataIsPassedToPayloadProcessor()
//         {
//             var payloadProcessor = new Mock<IPayloadProcessor>();
//             var sendQueueStore = new Mock<IClientStore>();
//             var queueStore = new Mock<ITopicStore>();
//             var messageStore = new Mock<IMessageStore>();
//             var serializer = new Mock<ISerializer>();
//
//             var coordinator = new Broker(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);
//             
//             coordinator.DataReceived(It.IsAny<Guid>(), It.IsAny<Memory<byte>>());
//             
//             payloadProcessor.Verify(p => p.OnDataReceived(It.IsAny<Guid>(), It.IsAny<Memory<byte>>()));
//         }
//
//         [Fact]
//         public void MakeSureWhenClientIsConnectedSendQueueStoreIsNotified()
//         {
//             var payloadProcessor = new Mock<IPayloadProcessor>();
//             var sendQueueStore = new Mock<IClientStore>();
//             var queueStore = new Mock<ITopicStore>();
//             var messageStore = new Mock<IMessageStore>();
//             var serializer = new Mock<ISerializer>();
//             var clintSession = new Mock<IClient>();
//             var sendQueue = new Mock<IQueue>();
//
//             sendQueueStore.Setup(sqs => sqs.Add(It.IsAny<IClient>(), null)).Returns(sendQueue.Object);
//
//             var coordinator = new Broker(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);
//
//             coordinator.ClientConnected(clintSession.Object);
//             
//             sendQueueStore.Verify(s => s.Add(It.IsAny<IClient>(), It.IsAny<IQueue>()));
//         }
//
//         [Fact]
//         public void MakeSureWhenClientIsDisconnectedSendQueueStoreIsNotified()
//         {
//             var payloadProcessor = new Mock<IPayloadProcessor>();
//             var sendQueueStore = new Mock<IClientStore>();
//             var queueStore = new Mock<ITopicStore>();
//             var messageStore = new Mock<IMessageStore>();
//             var serializer = new Mock<ISerializer>();
//
//             var coordinator = new Broker(payloadProcessor.Object, sendQueueStore.Object, queueStore.Object, messageStore.Object, serializer.Object);
//
//             coordinator.ClientDisconnected(It.IsAny<IClient>());
//             
//             sendQueueStore.Verify(s => s.Remove(It.IsAny<IClient>()));
//         }
//         
//     }
// }