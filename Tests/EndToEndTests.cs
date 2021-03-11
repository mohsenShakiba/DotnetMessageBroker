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
        [InlineData(50000, 10)]
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

            // declare the queue
            var declareResult = await subscriberClient.CreateQueueAsync(queueName, queueRoute);

            if (!declareResult.IsSuccess)
                throw new Exception($"declare queue failed with error {declareResult.InternalErrorCode}");

            // setup reset event
            var manualResetEvent = new ManualResetEventSlim(false);

            // setup queue 
            var queueManager = await subscriberClient.GetQueueSubscriber(queueName, queueRoute);

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
        [InlineData(1000, 10, 5)]
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

            // declare the queue
            var declareResult = await subscriberClient.CreateQueueAsync(queueName, queueRoute);

            if (!declareResult.IsSuccess)
                throw new Exception($"declare queue failed with error {declareResult.InternalErrorCode}");

            // setup reset event
            var manualResetEvent = new ManualResetEventSlim(false);

            // setup queue 
            var queueManager = await subscriberClient.GetQueueSubscriber(queueName, queueRoute);

            // setup subscriber on disconnect
            subscriberClient.OnDisconnected += () =>
            {
                Logger.LogInformation($"subscriber disconnected, reconnecting");
                subscriberClient.Reconnect();
            };

            // setup publisher on disconnect
            publisherClient.OnDisconnected += () =>
            {
                Logger.LogInformation($"publisher disconnected, reconnecting");
                publisherClient.Reconnect();
            };

            // setup subscriber
            queueManager.MessageReceived += async msg =>
            {
                var ratio = random.Next(0, 100);
                
                if (ratio < failureRatio)
                {
                    subscriberConnectionManager.SimulateInterrupt();
                    return;
                }

                if (ratio < nackRation)
                {
                    await subscriberClient.NackAsync(msg.MessageId);
                    return;
                }

                lock (messageStoreLock)
                {
                    var messageStr = Encoding.UTF8.GetString(msg.Data.Span);

                    messageStore[messageStr] -= 1;

                    var receivedMessagesCount = messageStore.Values.Count(v => v <= 0);

                    if (receivedMessagesCount == messageCount)
                        manualResetEvent.Set();
                    Logger.LogInformation($"received count: {receivedMessagesCount} with content: {messageStr}");
 
                }
                

                await subscriberClient.AckAsync(msg.MessageId);
            };

            var publishedMessages = 0;

            while (publishedMessages < messageCount)
            {
                if (!publisherClient.Connected)
                {
                    Thread.Sleep(2000);
                    Logger.LogInformation($"publisher disconnected, ignoreing");
                    continue;
                }
                var ratio = random.Next(0, 100);

                if (ratio < failureRatio)
                {
                    Logger.LogInformation($"interupting");
                    publisherConnectionManager.SimulateInterrupt();
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

                Logger.LogInformation($"before send {publishedMessages}");
                var publishResult = await publisherClient.PublishAsync(queueRoute, randomData);

                if (!publishResult.IsSuccess)
                {
                    messageStore.Remove(randomString);
                    Logger.LogInformation("failed to send data to server");
                    continue;                    
                }
                
                Logger.LogInformation($"sent {publishedMessages}");

                publishedMessages += 1;
            }

            Logger.LogInformation("finished sending message");
            
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
            
            // Logger.AddFileLogger(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\testo\logs.txt");
            Logger.AddConsole();

            return serviceCollection.BuildServiceProvider();
        }
    }
}