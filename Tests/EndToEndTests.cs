using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Core;
using MessageBroker.Core.Persistence.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tests.Classes;
using Xunit;

namespace Tests
{
    public class EndToEndTests
    {

        [Theory]
        [InlineData(100, 0.5f)]
        public async Task Receive_AllMessagesAreReceived_SubscriberIsDisconnectedMultipleTimes(int numberOfMessagesToBeReceived, float chanceOfClientFailure)
        {
            // declare variables
            var topicName = RandomGenerator.GenerateString(10);

            var loggerFactory = new LoggerFactory();
            var messageStore = new MessageStore(loggerFactory.CreateLogger<MessageStore>());
            messageStore.Setup(topicName, numberOfMessagesToBeReceived);
            var clientConnectionConfiguration = new ClientConnectionConfiguration
            {
                AutoReconnect = true,
                IpEndPoint = new IPEndPoint(IPAddress.Loopback, 8002)
            };

            // setup server
            var brokerBuilder = new BrokerBuilder();

            using var broker = brokerBuilder
                .UseMemoryStore()
                .UseEndPoint(clientConnectionConfiguration.IpEndPoint)
                 // .AddConsoleLog()
                .Build();

            broker.Start();

            await using var clientFactory = new BrokerClientFactory();

            // setup subscriber
            var subscriberClient = clientFactory.GetClient();
            subscriberClient.Connect(clientConnectionConfiguration);

            // declare topic
            var declareResult = await subscriberClient.DeclareTopicAsync(topicName, topicName);
            Assert.True(declareResult.IsSuccess);

            // setup a topic with test data
            PopulateTopicWithMessage(topicName, numberOfMessagesToBeReceived, messageStore, broker);

            // get new subscription 
            var subscription = await subscriberClient.GetTopicSubscriptionAsync(topicName, topicName);

            // setup subscriber
            subscription.MessageReceived += msg =>
            {
                if (RandomGenerator.GenerateDouble() < chanceOfClientFailure)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        subscriberClient.ConnectionManager.Socket.SimulateInterrupt();
                    });
                    return;
                }

                // note: the id of msg has changed 
                var messageData = new Guid(msg.Data.Span);

                messageStore.OnMessageReceived(messageData);

                msg.Ack();
            };

            messageStore.WaitForAllMessageToBeReceived();
        }


        [Theory]
        [InlineData(100, 0.5f)]
        public async Task Receive_AllMessagesAreReceived_SomeMessagesAreNacked(int numberOfMessagesToBeReceived, float changeForMessageToBeNacked)
        {
            // declare variables
            var topicName = RandomGenerator.GenerateString(10);
            var loggerFactory = new LoggerFactory();
            var messageStore = new MessageStore(loggerFactory.CreateLogger<MessageStore>());
            messageStore.Setup(topicName, numberOfMessagesToBeReceived);
            var clientConnectionConfiguration = new ClientConnectionConfiguration
            {
                AutoReconnect = true,
                IpEndPoint = new IPEndPoint(IPAddress.Loopback, 8002)
            };
            
            // setup server
            var brokerBuilder = new BrokerBuilder();
        
            using var broker = brokerBuilder
                .UseMemoryStore()
                .UseEndPoint(clientConnectionConfiguration.IpEndPoint)
                .Build();
            
            broker.Start();
        
            await using var clientFactory = new BrokerClientFactory();
            
            // setup subscriber
            var subscriberClient = clientFactory.GetClient();
            subscriberClient.Connect(clientConnectionConfiguration);
        
            // declare topic
            var declareResult = await subscriberClient.DeclareTopicAsync(topicName, topicName);
            Assert.True(declareResult.IsSuccess);
        
            // setup a topic with test data
            PopulateTopicWithMessage(topicName, numberOfMessagesToBeReceived, messageStore, broker);
            
            // get new subscription 
            var subscription = await subscriberClient.GetTopicSubscriptionAsync(topicName, topicName);
            
            // setup subscriber
            subscription.MessageReceived += msg =>
            {
                if (RandomGenerator.GenerateDouble() < changeForMessageToBeNacked)
                {
                    msg.Nack();
                    return;
                }
        
                // note: the id of msg has changed 
                var messageData = new Guid(msg.Data.Span);
        
                messageStore.OnMessageReceived(messageData);
            
                msg.Ack();
            };
        
            messageStore.WaitForAllMessageToBeReceived();
        }

        [Theory]
        [InlineData(100, 0.5f)]
        public async Task Send_AllMessagesAreSent_PublisherIsDisconnectedMultipleTimes(int numberOfMessagesToSend, float chanceOfClientFailure)
        {
            // in this test we will try to send n messages to server while keep disconnecting the client 
            // to make sure that messages will be received by the server and the acknowledge will be received 
            // by the client 
            
            // declare variables
            var topicName = RandomGenerator.GenerateString(10);
            var loggerFactory = new LoggerFactory();
            var messageStore = new MessageStore(loggerFactory.CreateLogger<MessageStore>());
            messageStore.Setup(topicName, numberOfMessagesToSend);
            var clientConnectionConfiguration = new ClientConnectionConfiguration
            {
                AutoReconnect = true,
                IpEndPoint = new IPEndPoint(IPAddress.Loopback, 8001)
            };
            
            // setup server
            var brokerBuilder = new BrokerBuilder();

            using var broker = brokerBuilder
                .UseMemoryStore()
                .UseEndPoint(clientConnectionConfiguration.IpEndPoint)
                .Build();
            
            broker.Start();
        
            await using var clientFactory = new BrokerClientFactory();
            
            // setup publisher
            var publisherClient = clientFactory.GetClient();
            publisherClient.Connect(clientConnectionConfiguration);
        
            // declare topic
            var declareResult = await publisherClient.DeclareTopicAsync(topicName, topicName);
            Assert.True(declareResult.IsSuccess);

            while (messageStore.SentCount < numberOfMessagesToSend)
            {
                if (RandomGenerator.GenerateDouble() < chanceOfClientFailure)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        publisherClient.ConnectionManager.Socket.SimulateInterrupt();
                    });
                }

                var msg = messageStore.NewMessage();

                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                
                var publishResult = await publisherClient.PublishRawAsync(msg, true, cancellationTokenSource.Token);
                
                if (publishResult.IsSuccess)
                {
                    messageStore.OnMessageSent(msg.Id);
                }

            }

            messageStore.WaitForAllMessageToBeSent();
        }


        private void PopulateTopicWithMessage(string topicName, int numberOfMessages, MessageStore messageStore, IBroker broker)
        {
            var topicStore = broker.ServiceProvider.GetRequiredService<ITopicStore>();

            topicStore.TryGetValue(topicName, out var topic);

            for (var i = 0; i < numberOfMessages; i++)
            {
                var message = messageStore.NewMessage();
                topic.OnMessage(message);
            }
        }
       
    }
}