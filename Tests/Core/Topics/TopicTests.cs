using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessageBroker.Common.Async;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Models;
using MessageBroker.Common.Serialization;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Dispatching;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.Topics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Core.Topics
{
    public class TopicTests
    {
        [Fact]
        public async Task OnMessage_SubscriptionExists_ClientEnqueueIsCalled()
        {
            var messageStore = new InMemoryMessageStore();
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var dispatcher = new DefaultDispatcher();

            var mockClient = new Mock<IClient>();
            mockClient.Setup(c => c.ReachedMaxConcurrency).Returns(false);
            mockClient.Setup(c => c.Enqueue(It.IsAny<SerializedPayload>())).Returns(new AsyncPayloadTicket());

            var activeSessionId = Guid.NewGuid();

            mockClient.Setup(c => c.Id).Returns(activeSessionId);

            var topic = new Topic(dispatcher,
                messageStore,
                routeMatcher,
                serializer,
                NullLogger<Topic>.Instance
            );

            topic.Setup("TEST", "TEST");

            topic.ClientSubscribed(mockClient.Object);

            var sampleMessage = RandomGenerator.GetMessage("TEST");

            topic.OnMessage(sampleMessage);

            await topic.ReadNextMessage();

            mockClient.Verify(cs => cs.Enqueue(It.IsAny<SerializedPayload>()));
        }

        [Fact]
        public async Task OnMessage_SubscriptionIsAddedAfterTheMessage_ClientEnqueueIsCalled()
        {
            var messageStore = new InMemoryMessageStore();
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var dispatcher = new DefaultDispatcher();

            var mockClient = new Mock<IClient>();
            mockClient.Setup(c => c.ReachedMaxConcurrency).Returns(false);
            mockClient.Setup(c => c.Enqueue(It.IsAny<SerializedPayload>())).Returns(new AsyncPayloadTicket());

            var activeSessionId = Guid.NewGuid();

            mockClient.Setup(c => c.Id).Returns(activeSessionId);

            var topic = new Topic(dispatcher,
                messageStore,
                routeMatcher,
                serializer,
                NullLogger<Topic>.Instance
            );

            topic.Setup("TEST", "TEST");

            var sampleMessage = RandomGenerator.GetMessage("TEST");

            topic.OnMessage(sampleMessage);

            topic.ClientSubscribed(mockClient.Object);

            await topic.ReadNextMessage();

            mockClient.Verify(cs => cs.Enqueue(It.IsAny<SerializedPayload>()));
        }

        [Fact]
        public void OnMessage_AnyCondition_MessageIsAddedToMessageStore()
        {
            var messageStore = new Mock<IMessageStore>();
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var dispatcher = new DefaultDispatcher();

            var topic = new Topic(dispatcher,
                messageStore.Object,
                routeMatcher,
                serializer,
                NullLogger<Topic>.Instance
            );

            topic.Setup("TEST", "TEST");

            var sampleMessage = RandomGenerator.GetMessage("TEST");

            topic.OnMessage(sampleMessage);

            messageStore.Verify(ms => ms.Add(It.IsAny<TopicMessage>()));
        }

        [Fact]
        public async Task OnNack_AnyCondition_RequeueMessage()
        {
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var dispatcher = new DefaultDispatcher();
            var messageStore = new InMemoryMessageStore();

            var client = new Mock<IClient>();
            client.Setup(c => c.ReachedMaxConcurrency).Returns(false);
            client.Setup(c => c.Enqueue(It.IsAny<SerializedPayload>())).Returns(new AsyncPayloadTicket());

            var topic = new Topic(dispatcher,
                messageStore,
                routeMatcher,
                serializer,
                NullLogger<Topic>.Instance
            );

            topic.Setup("TEST", "TEST");

            topic.ClientSubscribed(client.Object);

            var sampleMessage = RandomGenerator.GetMessage("TEST");

            topic.OnMessage(sampleMessage);

            await topic.ReadNextMessage();

            client.Verify(ms => ms.Enqueue(It.IsAny<SerializedPayload>()));

            topic.OnStatusChanged(sampleMessage.Id, false);

            await topic.ReadNextMessage();

            client.Verify(ms => ms.Enqueue(It.IsAny<SerializedPayload>()));
        }

        [Fact]
        public void OnAck_AnyCondition_MessageIsDeleted()
        {
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var dispatcher = new DefaultDispatcher();
            var messageStore = new Mock<IMessageStore>();

            var topic = new Topic(dispatcher,
                messageStore.Object,
                routeMatcher,
                serializer,
                NullLogger<Topic>.Instance
            );

            topic.Setup("TEST", "TEST");

            var sampleMessage = RandomGenerator.GetMessage("TEST");

            topic.OnMessage(sampleMessage);

            topic.OnStatusChanged(sampleMessage.Id, true);
            messageStore.Verify(ms => ms.Delete(sampleMessage.Id));
        }

        [Fact]
        public async Task Setup_MessagesExistInMessageStore_MessagesAreReceivedByClient()
        {
            var serializer = new Serializer();
            var routeMatcher = new RouteMatcher();
            var dispatcher = new DefaultDispatcher();
            var messageStore = new Mock<IMessageStore>();

            var client = new Mock<IClient>();
            client.Setup(c => c.ReachedMaxConcurrency).Returns(false);
            client.Setup(c => c.Enqueue(It.IsAny<SerializedPayload>())).Returns(new AsyncPayloadTicket());

            var sampleMessage = RandomGenerator.GetMessage("TEST");
            var topicMessage = sampleMessage.ToTopicMessage(sampleMessage.Route);

            messageStore.Setup(ms => ms.GetAll()).Returns(new List<Guid> {sampleMessage.Id});
            messageStore.Setup(ms => ms.TryGetValue(sampleMessage.Id, out topicMessage)).Returns(true);

            var topic = new Topic(dispatcher,
                messageStore.Object,
                routeMatcher,
                serializer,
                NullLogger<Topic>.Instance
            );

            topic.Setup("TEST", "TEST");

            topic.ClientSubscribed(client.Object);

            await topic.ReadNextMessage();

            client.Verify(c => c.Enqueue(It.IsAny<SerializedPayload>()));
        }
    }
}