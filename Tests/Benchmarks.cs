using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Classes;
using Xunit;

namespace Tests
{
    public class Benchmarks
    {
        [Theory]
        [InlineData(1000)]
        [InlineData(10_000)]
        [InlineData(100_000)]
        public async Task BenchmarkTest(int messageCount)
        {
            var topicName = RandomGenerator.GenerateString(10);
            var messageStore = new MessageStore(NullLogger<MessageStore>.Instance);
            var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 8100);
            var clientConnectionConfiguration = new ClientConnectionConfiguration
            {
                AutoReconnect = true,
                EndPoint = serverEndPoint
            };
            
            // setup message store
            messageStore.Setup(topicName, messageCount);
            
            // setup server
            using var broker = new BrokerBuilder()
                .UseMemoryStore()
                .UseEndPoint(serverEndPoint)
                .Build();

            broker.Start();
            
            var clientFactory = new BrokerClientFactory();
            
            // setup publisher
            await using var publisherClient = clientFactory.GetClient();
            publisherClient.Connect(clientConnectionConfiguration);
            
            // setup subscriber
            await using var subscriberClient = clientFactory.GetClient();
            subscriberClient.Connect(clientConnectionConfiguration);
            
            // declare topic
            var declareResult = await publisherClient.DeclareTopicAsync(topicName, topicName);
            Assert.True(declareResult.IsSuccess);

            // create subscription
            var subscription = await subscriberClient.GetTopicSubscriptionAsync(topicName);

            subscription.MessageReceived += msg =>
            {
                var messageIdentifier = new Guid(msg.Data.Span);
                
                messageStore.OnMessageReceived(messageIdentifier);
                
                msg.Ack();
            };
            
            // send messages to server
            while (messageStore.SentCount < messageCount)
            {
                var msg = messageStore.NewMessage();

                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

                var publishResult = await publisherClient.PublishAsync(topicName, msg.Data.ToArray(), cancellationTokenSource.Token);

                if (publishResult.IsSuccess) messageStore.OnMessageSent(msg.Id);
            }

            // wait for messages to be sent
            messageStore.WaitForAllMessageToBeSent();
            
            // wait for messages to be received
            messageStore.WaitForAllMessageToBeReceived();
        }
    }
}