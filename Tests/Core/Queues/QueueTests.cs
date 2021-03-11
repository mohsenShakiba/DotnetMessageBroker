using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Binary;
using MessageBroker.Core;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Messages.InMemoryStore;
using MessageBroker.Core.Queues;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.SessionPolicy;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;
using MessageBroker.TCP.Client;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Core.Queues
{
    public class QueueTests
    {
        [Fact]
        public void MakeSureWhenMessageIsAddedToQueueAndSessionExistsTheMessageWillBeSentToSendQueue()
        {
            var messageStore = new InMemoryMessageStore();
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();

            var clientSession = new Mock<IClientSession>();

            var activeSessionId = Guid.NewGuid();

            clientSession.Setup(c => c.Id).Returns(activeSessionId);

            var queue = new Queue(sessionPolicy,
                messageStore,
                sendQueueStore,
                routeMatcher,
                serializer);

            queue.Setup("TEST", "TEST");

            sendQueueStore.Add(clientSession.Object);
            queue.SessionSubscribed(activeSessionId);

            var sampleMessage = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = RandomGenerator.GenerateBytes(100)
            };

            queue.OnMessage(sampleMessage);
            
            queue.ReadNextMessage();

            clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void MakeSureWhenMessageIsAddedItIsSentToSendQueueWhenASessionIsAvailable()
        {
            var messageStore = new InMemoryMessageStore();
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();

            var clientSession = new Mock<IClientSession>();

            var activeSessionId = Guid.NewGuid();

            clientSession.Setup(c => c.Id).Returns(activeSessionId);

            var queue = new Queue(sessionPolicy,
                messageStore,
                sendQueueStore,
                routeMatcher,
                serializer);

            queue.Setup("TEST", "TEST");

            var sampleMessage = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = RandomGenerator.GenerateBytes(100)
            };

            queue.OnMessage(sampleMessage);
            
            sendQueueStore.Add(clientSession.Object);
            
            queue.SessionSubscribed(activeSessionId);
            
            queue.ReadNextMessage();
            
            clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void MakeSureMessageIsStoredInMessageStoreWhenAddedToQueue()
        {
            var messageStore = new Mock<IMessageStore>();
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();

            var queue = new Queue(sessionPolicy,
                messageStore.Object,
                sendQueueStore,
                routeMatcher,
                serializer);

            queue.Setup("TEST", "TEST");

            var route = RandomGenerator.GenerateString(10);
            var data = RandomGenerator.GenerateBytes(100);

            var sampleMessage = new Message
            {
                Id = It.IsAny<Guid>(),
                Route = route,
                Data = data
            };

            queue.OnMessage(sampleMessage);

            messageStore.Verify(ms => ms.Add(It.IsAny<QueueMessage>()));
        }
        
        [Fact]
        public void MakeSureWhenMessageIsNackedBySendQueueItWillBeReQueueTheMessage()
        {
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();
            var messageStore = new InMemoryMessageStore();

            var clientSession = new Mock<IClientSession>();

            var activeSessionId = Guid.NewGuid();
            var firstSendAsyncCall = true;
            
            clientSession.Setup(c => c.Id).Returns(activeSessionId);
            
            clientSession.Setup(c => c.SendAsync(It.IsAny<Memory<byte>>())).Returns(() =>
            {
                try
                {
                    if (firstSendAsyncCall)
                        return Task.FromResult(false);
                    else
                        return Task.FromResult(true);
                }
                finally
                {
                    firstSendAsyncCall = false;
                }
            });

            var queue = new Queue(sessionPolicy,
                messageStore,
                sendQueueStore,
                routeMatcher,
                serializer);

            queue.Setup("TEST", "TEST");

            sendQueueStore.Add(clientSession.Object);
            queue.SessionSubscribed(activeSessionId);

            var sampleMessage = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = RandomGenerator.GenerateBytes(100)
            };

            queue.OnMessage(sampleMessage);

            queue.ReadNextMessage();
            
            clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
            
            queue.ReadNextMessage();
            
            clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void MakeSureWhenMessageIsAckedBySendQueueItWillRemoveMessageFromMessageStore()
        {
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();
            
            var messageStore = new Mock<IMessageStore>();
            var clientSession = new Mock<IClientSession>();

            var activeSessionId = Guid.NewGuid();
            
            var sampleMessage = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = RandomGenerator.GenerateBytes(100)
            };

            var queueMessage = sampleMessage.ToQueueMessage("TEST");
            
            clientSession.Setup(c => c.Id).Returns(activeSessionId);
            clientSession.Setup(c => c.SendAsync(It.IsAny<Memory<byte>>())).Returns(Task.FromResult(true));
            messageStore.Setup(ms => ms.TryGetValue(It.IsAny<Guid>(), out queueMessage)).Returns(true);

            var queue = new Queue(sessionPolicy,
                messageStore.Object,
                sendQueueStore,
                routeMatcher,
                serializer);

            queue.Setup("TEST", "TEST");

            sendQueueStore.Add(clientSession.Object);
            queue.SessionSubscribed(activeSessionId);

            if (sendQueueStore.TryGet(activeSessionId, out var sendQueue))
            {
                sendQueue.Configure(10, true);
            }

            queue.OnMessage(sampleMessage);

            queue.ReadNextMessage();
            
            sendQueue.
            
            clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
            messageStore.Verify(cs => cs.Delete(It.IsAny<Guid>()));
        }

        [Fact]
        public void MakeSureWhenSessionIsSubscribedItIsAddedToSessionPolicyAndWhenItIsDisconnectedItWillBeRemovedFromSessionPolicy()
        {
            var messageStore = new InMemoryMessageStore();
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();

            var queue = new Queue(sessionPolicy,
                messageStore,
                sendQueueStore,
                routeMatcher,
                serializer);

            queue.Setup("TEST", "TEST");

            var sessionId = Guid.NewGuid();
            
            Assert.False(sessionPolicy.HasSession());
            
            queue.SessionSubscribed(sessionId);
            
            Assert.True(sessionPolicy.HasSession());
            
            queue.SessionUnSubscribed(sessionId);
            
            Assert.False(sessionPolicy.HasSession());
        }

        [Fact]
        public void MakeSurePendingMessageInMessageStoreAreSentWhenQueueIsCreated()
        {
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();
            
            var messageStore = new Mock<IMessageStore>();
            var clientSession = new Mock<IClientSession>();

            var activeSessionId = Guid.NewGuid();
            
            var sampleMessage = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = RandomGenerator.GenerateBytes(100)
            };

            var queueMessage = sampleMessage.ToQueueMessage("TEST");
            
            clientSession.Setup(c => c.Id).Returns(activeSessionId);
            clientSession.Setup(c => c.SendAsync(It.IsAny<Memory<byte>>())).Returns(Task.FromResult(true));
            messageStore.Setup(ms => ms.TryGetValue(It.IsAny<Guid>(), out queueMessage)).Returns(true);
            messageStore.Setup(ms => ms.PendingMessages(It.IsAny<int>())).Returns(new [] {sampleMessage.Id});

            var queue = new Queue(sessionPolicy,
                messageStore.Object,
                sendQueueStore,
                routeMatcher,
                serializer);

            queue.Setup("TEST", "TEST");

            sendQueueStore.Add(clientSession.Object);
            queue.SessionSubscribed(activeSessionId);

            queue.ReadNextMessage();

            clientSession.Verify(cs => cs.SendAsync(It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void MakeSureRouteMatchingReturnsCorrectResultForMessageRoute()
        {
            var routeMatcher = new Mock<IRouteMatcher>();
            
            var messageStore = new InMemoryMessageStore();
            var serializer = new Serializer();
            var sendQueueStore = new SendQueueStore();
            var sessionPolicy = new RoundRobinSessionPolicy();

            var queue = new Queue(sessionPolicy,
                messageStore,
                sendQueueStore,
                routeMatcher.Object,
                serializer);

            queue.Setup("TEST", "TEST");

            queue.MessageRouteMatch(It.IsAny<string>());
            
            routeMatcher.Verify(rm => rm.Match(It.IsAny<string>(), It.IsAny<string>()));
        }
    }
}