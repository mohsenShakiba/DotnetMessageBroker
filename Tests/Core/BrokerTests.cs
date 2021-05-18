using System;
using System.Threading;
using MessageBroker.Common.Tcp;
using MessageBroker.Core;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Clients.Store;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Core
{
    public class BrokerTests
    {
        [Fact]
        public void Start_AnyCondition_SetupIsCalledForStores()
        {
            var listener = new Mock<IListener>();
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var clientStore = new Mock<IClientStore>();
            var topicStore = new Mock<ITopicStore>();
            var messageStore = new Mock<IMessageStore>();
            var logger = NullLogger<Broker>.Instance;

            var coordinator = new Broker(listener.Object, payloadProcessor.Object, clientStore.Object,
                topicStore.Object, messageStore.Object, CreateTestProvider(), logger);

            coordinator.Start();

            topicStore.Verify(m => m.Setup());
            messageStore.Verify(m => m.Setup());
        }

        [Fact]
        public void Start_DataReceived_DataIsPassedToPayloadProcessor()
        {
            var listener = new TestListener();
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var clientStore = new Mock<IClientStore>();
            var topicStore = new Mock<ITopicStore>();
            var messageStore = new Mock<IMessageStore>();
            var logger = NullLogger<Broker>.Instance;

            var broker = new Broker(listener, payloadProcessor.Object, clientStore.Object,
                topicStore.Object, messageStore.Object, CreateTestProvider(), logger);

            broker.Start();

            var testSocket = listener.CreateTestSocket();

            listener.AcceptTestSocket(testSocket);

            var testData = RandomGenerator.GetMessageSerializedPayload();

            testSocket.SendTestData(testData.Data);

            Thread.Sleep(1000);

            payloadProcessor.Verify(p => p.OnDataReceived(It.IsAny<Guid>(), It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void Start_ClientAccepted_ClientStoreIsNotifier()
        {
            var listener = new TestListener();
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var clientStore = new Mock<IClientStore>();
            var topicStore = new Mock<ITopicStore>();
            var messageStore = new Mock<IMessageStore>();
            var logger = NullLogger<Broker>.Instance;

            var broker = new Broker(listener, payloadProcessor.Object, clientStore.Object,
                topicStore.Object, messageStore.Object, CreateTestProvider(), logger);

            broker.Start();

            var testSocket = listener.CreateTestSocket();

            listener.AcceptTestSocket(testSocket);

            Thread.Sleep(1000);

            clientStore.Verify(p => p.Add(It.IsAny<IClient>()));
        }


        [Fact]
        public void Start_ClientDisconnected_ClientStoreIsNotifier()
        {
            var listener = new TestListener();
            var payloadProcessor = new Mock<IPayloadProcessor>();
            var clientStore = new Mock<IClientStore>();
            var topicStore = new Mock<ITopicStore>();
            var messageStore = new Mock<IMessageStore>();
            var logger = NullLogger<Broker>.Instance;

            var broker = new Broker(listener, payloadProcessor.Object, clientStore.Object,
                topicStore.Object, messageStore.Object, CreateTestProvider(), logger);

            broker.Start();

            var testSocket = listener.CreateTestSocket();

            listener.AcceptTestSocket(testSocket);

            testSocket.Disconnect();

            Thread.Sleep(1000);

            clientStore.Verify(p => p.Remove(It.IsAny<IClient>()));
        }

        private IServiceProvider CreateTestProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddTransient<IClient, Client>();
            return serviceCollection.BuildServiceProvider();
        }
    }
}