// using System;
// using System.Threading.Tasks;
// using MessageBroker.Core;
// using MessageBroker.Core.Persistence.Messages;
// using MessageBroker.Core.Persistence.Messages.InMemoryStore;
// using MessageBroker.Core.Queues;
// using MessageBroker.Core.Queues.Store;
// using MessageBroker.Core.RouteMatching;
// using MessageBroker.Core.SessionPolicy;
// using MessageBroker.Core.Topics;
// using MessageBroker.Models;
// using MessageBroker.Serialization;
// using MessageBroker.TCP.Client;
// using Moq;
// using Tests.Classes;
// using Xunit;
//
// namespace Tests.Core.Queues
// {
//     public class QueueTests
//     {
//         [Fact]
//         public async Task MakeSureWhenMessageIsAddedToQueueAndSessionExistsTheMessageWillBeSentToSendQueue()
//         {
//             var messageStore = new InMemoryMessageStore();
//             var serializer = new Serializer();
//             var routeMatcher = new RouteMatcher();
//             var sendQueueStore = new ClientStore();
//             var sessionPolicy = new DefaultDispatchPolicy();
//
//             var clientSession = new Mock<IClient>();
//
//             var activeSessionId = Guid.NewGuid();
//
//             clientSession.Setup(c => c.Id).Returns(activeSessionId);
//
//             var queue = new Topic(sessionPolicy,
//                 messageStore,
//                 sendQueueStore,
//                 routeMatcher,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             var sendQueue = sendQueueStore.Add(clientSession.Object);
//             queue.ClientSubscribed(activeSessionId);
//
//             var sampleMessage = new Message
//             {
//                 Id = Guid.NewGuid(),
//                 Route = "TEST",
//                 Data = RandomGenerator.GenerateBytes(100)
//             };
//
//             queue.OnMessage(sampleMessage);
//
//             await queue.ReadNextMessage();
//
//             await Task.Delay(100);
//
//             await sendQueue.ReadNextPayloadAsync();
//
//             clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
//         }
//
//         [Fact]
//         public async Task MakeSureWhenMessageIsAddedItIsSentToSendQueueWhenASessionIsAvailable()
//         {
//             var messageStore = new InMemoryMessageStore();
//             var serializer = new Serializer();
//             var routeMatcher = new RouteMatcher();
//             var sendQueueStore = new ClientStore();
//             var sessionPolicy = new DefaultDispatchPolicy();
//
//             var clientSession = new Mock<IClient>();
//
//             var activeSessionId = Guid.NewGuid();
//
//             clientSession.Setup(c => c.Id).Returns(activeSessionId);
//
//             var queue = new Topic(sessionPolicy,
//                 messageStore,
//                 sendQueueStore,
//                 routeMatcher,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             var sampleMessage = new Message
//             {
//                 Id = Guid.NewGuid(),
//                 Route = "TEST",
//                 Data = RandomGenerator.GenerateBytes(100)
//             };
//
//             queue.OnMessage(sampleMessage);
//
//             var sendQueue = sendQueueStore.Add(clientSession.Object);
//
//             queue.ClientSubscribed(activeSessionId);
//
//             await queue.ReadNextMessage();
//
//             await sendQueue.ReadNextPayloadAsync();
//
//             clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
//         }
//
//         [Fact]
//         public void MakeSureMessageIsStoredInMessageStoreWhenAddedToQueue()
//         {
//             var messageStore = new Mock<IMessageStore>();
//             var serializer = new Serializer();
//             var routeMatcher = new RouteMatcher();
//             var sendQueueStore = new ClientStore();
//             var sessionPolicy = new DefaultDispatchPolicy();
//
//             var queue = new Topic(sessionPolicy,
//                 messageStore.Object,
//                 sendQueueStore,
//                 routeMatcher,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             var route = RandomGenerator.GenerateString(10);
//             var data = RandomGenerator.GenerateBytes(100);
//
//             var sampleMessage = new Message
//             {
//                 Id = It.IsAny<Guid>(),
//                 Route = route,
//                 Data = data
//             };
//
//             queue.OnMessage(sampleMessage);
//
//             messageStore.Verify(ms => ms.Add(It.IsAny<QueueMessage>()));
//         }
//
//         [Fact]
//         public async Task MakeSureWhenMessageIsNackedBySendQueueItWillBeReQueueTheMessage()
//         {
//             var serializer = new Serializer();
//             var routeMatcher = new RouteMatcher();
//             var sendQueueStore = new ClientStore();
//             var sessionPolicy = new DefaultDispatchPolicy();
//             var messageStore = new InMemoryMessageStore();
//
//             var clientSession = new Mock<IClient>();
//
//             var activeSessionId = Guid.NewGuid();
//             var firstSendAsyncCall = true;
//
//             clientSession.Setup(c => c.Id).Returns(activeSessionId);
//
//             clientSession.Setup(c => c.SendAsync(It.IsAny<Memory<byte>>())).Returns(() =>
//             {
//                 try
//                 {
//                     if (firstSendAsyncCall)
//                         return Task.FromResult(false);
//                     else
//                         return Task.FromResult(true);
//                 }
//                 finally
//                 {
//                     firstSendAsyncCall = false;
//                 }
//             });
//
//             var queue = new Topic(sessionPolicy,
//                 messageStore,
//                 sendQueueStore,
//                 routeMatcher,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             var sendQueue = sendQueueStore.Add(clientSession.Object);
//             queue.ClientSubscribed(activeSessionId);
//
//             var sampleMessage = new Message
//             {
//                 Id = Guid.NewGuid(),
//                 Route = "TEST",
//                 Data = RandomGenerator.GenerateBytes(100)
//             };
//
//             queue.OnMessage(sampleMessage);
//
//             await queue.ReadNextMessage();
//
//             await sendQueue.ReadNextPayloadAsync();
//
//             clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
//
//             await queue.ReadNextMessage();
//
//             await sendQueue.ReadNextPayloadAsync();
//
//
//             clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
//         }
//
//         [Fact]
//         public async Task MakeSureWhenMessageIsAckedBySendQueueItWillRemoveMessageFromMessageStore()
//         {
//             var serializer = new Serializer();
//             var routeMatcher = new RouteMatcher();
//             var sendQueueStore = new ClientStore();
//             var sessionPolicy = new DefaultDispatchPolicy();
//
//             var messageStore = new Mock<IMessageStore>();
//             var clientSession = new Mock<IClient>();
//
//             var activeSessionId = Guid.NewGuid();
//
//             var sampleMessage = new Message
//             {
//                 Id = Guid.NewGuid(),
//                 Route = "TEST",
//                 Data = RandomGenerator.GenerateBytes(100)
//             };
//
//             var queueMessage = sampleMessage.ToQueueMessage("TEST");
//
//             clientSession.Setup(c => c.Id).Returns(activeSessionId);
//             clientSession.Setup(c => c.SendAsync(It.IsAny<Memory<byte>>())).Returns(Task.FromResult(true));
//             messageStore.Setup(ms => ms.TryGetValue(It.IsAny<Guid>(), out queueMessage)).Returns(true);
//
//             var queue = new Topic(sessionPolicy,
//                 messageStore.Object,
//                 sendQueueStore,
//                 routeMatcher,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             sendQueueStore.Add(clientSession.Object);
//             queue.ClientSubscribed(activeSessionId);
//
//             var foundSendQueue = sendQueueStore.TryGet(activeSessionId, out var sendQueue);
//
//             Assert.True(foundSendQueue);
//             
//             queue.OnMessage(sampleMessage);
//
//             await queue.ReadNextMessage();
//
//             await sendQueue.ReadNextPayloadAsync();
//
//             clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
//             messageStore.Verify(cs => cs.Delete(It.IsAny<Guid>()));
//         }
//
//         [Fact]
//         public void
//             MakeSureWhenSessionIsSubscribedItIsAddedToSessionPolicyAndWhenItIsDisconnectedItWillBeRemovedFromSessionPolicy()
//         {
//             var messageStore = new InMemoryMessageStore();
//             var serializer = new Serializer();
//             var routeMatcher = new RouteMatcher();
//             var sessionPolicy = new Mock<IDispatchPolicy>();
//             var sendQueueStoreMock = new Mock<IClientStore>();
//             var sendQueueMock = new Mock<IQueue>();
//             var sendQueue = sendQueueMock.Object;
//
//             sendQueueStoreMock.Setup(sqs => sqs.TryGet(It.IsAny<Guid>(), out sendQueue)).Returns(true);
//
//             var queue = new Topic(sessionPolicy.Object,
//                 messageStore,
//                 sendQueueStoreMock.Object,
//                 routeMatcher,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             var sessionId = Guid.NewGuid();
//
//             queue.ClientSubscribed(sessionId);
//
//             sessionPolicy.Verify(s => s.Add(It.IsAny<IQueue>(), It.IsAny<int>()));
//
//             queue.ClientUnsubscribed(sessionId);
//
//             sessionPolicy.Verify(s => s.Remove(It.IsAny<Guid>()));
//         }
//
//         [Fact]
//         public async Task MakeSurePendingMessageInMessageStoreAreSentWhenQueueIsCreated()
//         {
//             var serializer = new Serializer();
//             var routeMatcher = new RouteMatcher();
//             var sendQueueStore = new ClientStore();
//             var sessionPolicy = new DefaultDispatchPolicy();
//
//             var messageStore = new Mock<IMessageStore>();
//             var clientSession = new Mock<IClient>();
//
//             var activeSessionId = Guid.NewGuid();
//
//             var sampleMessage = new Message
//             {
//                 Id = Guid.NewGuid(),
//                 Route = "TEST",
//                 Data = RandomGenerator.GenerateBytes(100)
//             };
//
//             var queueMessage = sampleMessage.ToQueueMessage("TEST");
//
//             clientSession.Setup(c => c.Id).Returns(activeSessionId);
//             clientSession.Setup(c => c.SendAsync(It.IsAny<Memory<byte>>())).Returns(Task.FromResult(true));
//             messageStore.Setup(ms => ms.TryGetValue(It.IsAny<Guid>(), out queueMessage)).Returns(true);
//             messageStore.Setup(ms => ms.PendingMessages(It.IsAny<int>())).Returns(new[] {sampleMessage.Id});
//
//             var queue = new Topic(sessionPolicy,
//                 messageStore.Object,
//                 sendQueueStore,
//                 routeMatcher,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             var sendQueue = sendQueueStore.Add(clientSession.Object);
//             queue.ClientSubscribed(activeSessionId);
//
//             await queue.ReadNextMessage();
//
//             await sendQueue.ReadNextPayloadAsync();
//
//             clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
//         }
//
//         [Fact]
//         public void MakeSureRouteMatchingReturnsCorrectResultForMessageRoute()
//         {
//             var routeMatcher = new Mock<IRouteMatcher>();
//
//             var messageStore = new InMemoryMessageStore();
//             var serializer = new Serializer();
//             var sendQueueStore = new ClientStore();
//             var sessionPolicy = new DefaultDispatchPolicy();
//
//             var queue = new Topic(sessionPolicy,
//                 messageStore,
//                 sendQueueStore,
//                 routeMatcher.Object,
//                 serializer);
//
//             queue.Setup("TEST", "TEST");
//
//             queue.MessageRouteMatch(It.IsAny<string>());
//
//             routeMatcher.Verify(rm => rm.Match(It.IsAny<string>(), It.IsAny<string>()));
//         }
//     }
// }