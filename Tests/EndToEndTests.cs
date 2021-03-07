using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.Subscription;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.Core;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Messages.InMemoryStore;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Core.Queues;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.SessionPolicy;
using MessageBroker.Serialization;
using MessageBroker.Serialization.Pools;
using MessageBroker.TCP;
using MessageBroker.TCP.Client;
using MessageBroker.TCP.Server;
using Microsoft.Extensions.DependencyInjection;
using Tests.Classes;
using Xunit;

namespace Tests
{
    public class EndToEndTests
    {
        [Theory]
        [InlineData(1000, 10)]
        public async Task EndToEndTest_SingleSubscriberSinglePublisherNoInterrupt_AllMessagesAreReceivedBySubscriber(
            int messageCount, int nackRation)
        {
            // declare variables
            var messageStore = new Dictionary<string, int>();
            var random = new Random();
            var messageStoreLock = new object();
            var queueName = RandomGenerator.GenerateString(10);
            var queueRoute = RandomGenerator.GenerateString(10);
            var destination = new IPEndPoint(IPAddress.Loopback, 8000);
            var serverServiceProvider = GetServerServiceProvider();
            var subscriberServiceProvider = GetClientServiceProvider();
            var publisherServiceProvider = GetClientServiceProvider();

            // setup server
            var server = serverServiceProvider.GetRequiredService<ISocketServer>();
            server.Start(destination);

            // setup send client
            var subscriberClient = subscriberServiceProvider.GetRequiredService<MessageBrokerClient>();
            subscriberClient.Connect(destination);

            // setup receive client
            var publisherClient = publisherServiceProvider.GetRequiredService<MessageBrokerClient>();
            publisherClient.Connect(destination);

            // setup reset event
            var manualResetEvent = new ManualResetEventSlim(false);

            // setup queue 
            var queueManager = subscriberClient.GetQueueSubscriber(queueName, queueRoute);
            var declareResult = await queueManager.DeclareQueue();
            var subscribeResult = await queueManager.SubscribeQueue();

            if (!declareResult.IsSuccess)
                throw new Exception($"declare queue failed with error {declareResult.InternalErrorCode}");

            if (!subscribeResult.IsSuccess)
                throw new Exception($"subscription failed with error {declareResult.InternalErrorCode}");

            // setup subscriber
            queueManager.MessageReceived += async msg =>
            {
                var ratio = random.Next(0, 100);

                if (ratio < nackRation)
                {
                    await subscriberClient.NackAsync(msg.MessageId);
                    return;
                }

                lock (messageStoreLock)
                {
                    var messageStr = Encoding.UTF8.GetString(msg.Data.Span);

                    if (messageStore.ContainsKey(messageStr))
                    {
                        messageStore[messageStr] -= 1;
                    }

                    var receivedMessagesCount = messageStore.Values.Count(v => v == 0);

                    if (receivedMessagesCount == messageCount)
                        manualResetEvent.Set();
                }

                await subscriberClient.AckAsync(msg.MessageId);
            };

            for (var i = 0; i < messageCount; i++)
            {
                var randomString = RandomGenerator.GenerateString(10);
                var randomData = Encoding.UTF8.GetBytes(randomString);

                lock (messageStoreLock)
                {
                    if (messageStore.ContainsKey(randomString))
                        messageStore[randomString] += 1;
                    else
                        messageStore[randomString] = 1;
                }

                var publishResult = await publisherClient.PublishAsync(queueRoute, randomData);

                if (!publishResult.IsSuccess)
                    throw new Exception($"publish message failed with error {publishResult.InternalErrorCode}");
            }

            manualResetEvent.Wait();
            server.Stop();
        }


        [Theory]
        [InlineData(1000, 10, 10)]
        public async Task
            EndToEndTest_SingleSubscriberSinglePublisherWithInterrupts_AllMessagesAreReceivedBySubscriber(
                int messageCount, int nackRation, int failureRatio)
        {
            // declare variables
            var messageStore = new Dictionary<string, int>();
            var random = new Random();
            var messageStoreLock = new object();
            var queueName = RandomGenerator.GenerateString(10);
            var queueRoute = RandomGenerator.GenerateString(10);
            var destination = new IPEndPoint(IPAddress.Loopback, 8001);
            var serverServiceProvider = GetServerServiceProvider();
            var subscriberServiceProvider = GetClientServiceProvider();
            var publisherServiceProvider = GetClientServiceProvider();

            // setup server
            var server = serverServiceProvider.GetRequiredService<ISocketServer>();
            server.Start(destination);

            // setup send client
            var subscriberClient = subscriberServiceProvider.GetRequiredService<MessageBrokerClient>();
            subscriberClient.Connect(destination);

            // get the connection manager for subscriber and publisher
            var subscriberConnectionManager = subscriberServiceProvider.GetRequiredService<IConnectionManager>();
            var publisherConnectionManager = publisherServiceProvider.GetRequiredService<IConnectionManager>();

            // setup receive client
            var publisherClient = publisherServiceProvider.GetRequiredService<MessageBrokerClient>();
            publisherClient.Connect(destination);

            // setup reset event
            var manualResetEvent = new ManualResetEventSlim(false);

            // setup queue 
            var queueManager = subscriberClient.GetQueueSubscriber(queueName, queueRoute);
            var declareResult = await queueManager.DeclareQueue();
            var subscribeResult = await queueManager.SubscribeQueue();

            if (!declareResult.IsSuccess)
                throw new Exception($"declare queue failed with error {declareResult.InternalErrorCode}");

            if (!subscribeResult.IsSuccess)
                throw new Exception($"subscription failed with error {declareResult.InternalErrorCode}");

            // setup subscriber
            queueManager.MessageReceived += async msg =>
            {
                var ratio = random.Next(0, 100);

                if (ratio < nackRation)
                {
                    await subscriberClient.NackAsync(msg.MessageId);
                    return;
                }

                if (ratio < failureRatio)
                {
                    subscriberConnectionManager.SimulateInterrupt();
                    return;
                }

                lock (messageStoreLock)
                {
                    var messageStr = Encoding.UTF8.GetString(msg.Data.Span);

                    messageStore[messageStr] -= 1;

                    var receivedMessagesCount = messageStore.Values.Count(v => v <= 0);

                    if (receivedMessagesCount == messageCount)
                        manualResetEvent.Set();
                }

                await subscriberClient.AckAsync(msg.MessageId);
            };

            var publishedMessages = 0;

            while (publishedMessages < messageCount)
            {
                var ratio = random.Next(0, 100);

                if (ratio < failureRatio)
                {
                    publisherConnectionManager.SimulateInterrupt();
                    continue;
                }

                var randomString = RandomGenerator.GenerateString(10);
                var randomData = Encoding.UTF8.GetBytes(randomString);

                lock (messageStoreLock)
                {
                    if (messageStore.ContainsKey(randomString))
                        messageStore[randomString] += 1;
                    else
                        messageStore[randomString] = 1;
                }

                var publishResult = await publisherClient.PublishAsync(queueRoute, randomData);

                if (!publishResult.IsSuccess)
                    throw new Exception($"publish message failed with error {publishResult.InternalErrorCode}");
                
                publishedMessages += 1;
            }

            manualResetEvent.Wait();
            server.Stop();
        }

        // EndToEndTest_MultipleSubscribersMultiplePublishersNoInterrupt_AllMessagesAreReceivedBuSubscriber
        // EndToEndTest_MultipleSubscribersMultiplePublishersWithInterrupts_AllMessagesAreReceivedBuSubscriber

        private IServiceProvider GetServerServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IPayloadProcessor, PayloadProcessor>();
            serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
            serviceCollection.AddTransient<IClientSession, ClientSession>();
            serviceCollection.AddSingleton<ISerializer, Serializer>();
            serviceCollection.AddSingleton<IRouteMatcher, RouteMatcher>();
            serviceCollection.AddSingleton<ISocketServer, TcpSocketServer>();
            serviceCollection.AddTransient<ISessionPolicy, RoundRobinSessionPolicy>();
            serviceCollection.AddSingleton<IQueueStore, InMemoryQueueStore>();
            serviceCollection.AddTransient<IQueue, Queue>();
            serviceCollection.AddSingleton<Coordinator>();
            serviceCollection.AddSingleton<ISocketEventProcessor>(p => p.GetRequiredService<Coordinator>());
            serviceCollection.AddSingleton<ISocketDataProcessor>(p => p.GetRequiredService<Coordinator>());
            serviceCollection.AddSingleton<StringPool>();
            serviceCollection.AddTransient<IBinaryDataProcessor, BinaryDataProcessor>();
            serviceCollection.AddSingleton<ISendQueueStore, SendQueueStore>();

            return serviceCollection.BuildServiceProvider();
        }

        private IServiceProvider GetClientServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ISerializer, Serializer>();
            serviceCollection.AddSingleton<ISendPayloadTaskManager, SendPayloadTaskManager>();
            serviceCollection.AddSingleton<IConnectionManager, ConnectionManager>();
            serviceCollection.AddSingleton<IClientSession, ClientSession>();
            serviceCollection.AddSingleton<IBinaryDataProcessor, BinaryDataProcessor>();
            serviceCollection.AddSingleton<IReceiveDataProcessor, ReceiveDataProcessor>();
            serviceCollection.AddSingleton<ISubscriberStore, SubscriberStore>();
            serviceCollection.AddTransient<ISubscriber, Subscriber>();
            serviceCollection.AddSingleton<StringPool>();
            serviceCollection.AddSingleton<MessageBrokerClient>();
            serviceCollection.AddSingleton<ISendQueueStore, SendQueueStore>();

            Logger.AddFileLogger(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\testo\logs.txt");

            return serviceCollection.BuildServiceProvider();
        }
    }
}