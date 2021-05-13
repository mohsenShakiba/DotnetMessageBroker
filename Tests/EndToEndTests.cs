using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Common.Logging;
using MessageBroker.Core.Broker;
using MessageBroker.TCP;
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
            var messageStore = new MessageStore(topicName, numberOfMessagesToBeReceived);
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
                 .AddFile(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\Logs\test.txt")
                .Build();

            broker.Start();

            await using var clientFactory = new BrokerClientFactory();

            // setup subscriber
            var subscriberClient = clientFactory.GetClient();
            subscriberClient.Connect(clientConnectionConfiguration, true);

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
            var messageStore = new MessageStore(topicName, numberOfMessagesToBeReceived);
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
            subscriberClient.Connect(clientConnectionConfiguration, true);
        
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
            var messageStore = new MessageStore(topicName, numberOfMessagesToSend);
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
            var topic = broker.GetTopic(topicName);

            for(var i = 0; i < numberOfMessages; i++)
            {
                var message = messageStore.NewMessage();
                topic.OnMessage(message);
            }
        }
       
    }
}