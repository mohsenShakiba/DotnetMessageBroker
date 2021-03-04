using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.QueueManagement;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
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
using System.Linq;
using MessageBroker.Common.Logging;
using Xunit;

namespace Tests.Client
{
    public class ClientTests
    {

        [Theory]
        [InlineData(1000, 10)]
        public async Task EndToEndTest_SingleSubscriberSinglePublisherNoInterrupt_AllMessagesAreReceivedBySubscriber(int messageCount, int nackRation)
        {
            
            // declare variables
            var messageStore = new Dictionary<string, int>();
            var random = new Random();
            var messageStoreLock = new object();
            var queueName = RandomGenerator.GenerateString(10);
            var queueRoute = RandomGenerator.GenerateString(10);
            var destination = new IPEndPoint(IPAddress.Loopback, 8000);
            var serverServiceProvider = GetServerServiceProvider();
            var clientServiceProvider = GetClientServiceProvider();
            var clientServiceProvider2 = GetClientServiceProvider();
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = destination,
                RetryOnFailure = true
            };
            
            // setup server
            var server = serverServiceProvider.GetRequiredService<ISocketServer>();
            server.Start(destination);
            
            // setup send client
            var subscriberClient = clientServiceProvider.GetRequiredService<MessageBrokerClient>();
            subscriberClient.Connect(connectionConfiguration);
            
            // setup receive client
            var publisherClient = clientServiceProvider2.GetRequiredService<MessageBrokerClient>();
            publisherClient.Connect(connectionConfiguration);

            // setup reset event
            var manualResetEvent = new ManualResetEventSlim(false);
            
            // setup queue 
            var queueManager = subscriberClient.GetQueueManager(queueName, queueRoute);
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
        }

        [Fact]
        public async Task ContinuesTest()
        {
            for (var i = 0; i < 10; i++)
            {
                await EndToEndTest_SingleSubscriberSinglePublisherWithSubscriberInterrupts_AllMessagesAreReceivedBySubscriber(100, 10);
            }
        }

        [Theory]
        [InlineData(100, 10)]
        public async Task EndToEndTest_SingleSubscriberSinglePublisherWithSubscriberInterrupts_AllMessagesAreReceivedBySubscriber(int messageCount, int nackRation)
        {
            // declare variables
            var messageStore = new Dictionary<string, int>();
            var random = new Random();
            var messageStoreLock = new object();
            var queueName = RandomGenerator.GenerateString(10);
            var queueRoute = RandomGenerator.GenerateString(10);
            var destination = new IPEndPoint(IPAddress.Loopback, 8001);
            var serverServiceProvider = GetServerServiceProvider();
            var clientServiceProvider = GetClientServiceProvider();
            var clientServiceProvider2 = GetClientServiceProvider();
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = destination,
                RetryOnFailure = true
            };
            
            // setup server
            var server = serverServiceProvider.GetRequiredService<ISocketServer>();
            server.Start(destination);
            
            // setup send client
            var subscriberClient = clientServiceProvider.GetRequiredService<MessageBrokerClient>();
            subscriberClient.Connect(connectionConfiguration);
            
            // get the connection manager for subscriber
            var subscriberConnectionManager = clientServiceProvider.GetRequiredService<IConnectionManager>();
            
            // setup receive client
            var publisherClient = clientServiceProvider2.GetRequiredService<MessageBrokerClient>();
            publisherClient.Connect(connectionConfiguration);

            // setup reset event
            var manualResetEvent = new ManualResetEventSlim(false);
            
            // setup queue 
            var queueManager = subscriberClient.GetQueueManager(queueName, queueRoute);
            var declareResult = await queueManager.DeclareQueue();
            var subscribeResult = await queueManager.SubscribeQueue();
            
            if (!declareResult.IsSuccess)
                throw new Exception($"declare queue failed with error {declareResult.InternalErrorCode}");
            
            if (!subscribeResult.IsSuccess)
                throw new Exception($"subscription failed with error {declareResult.InternalErrorCode}");

            subscriberConnectionManager.OnClientConnected += () =>
            {
                queueManager.SubscribeQueue();
            };

            var shit = 0;
            
            // setup subscriber
            queueManager.MessageReceived += async msg =>
            {
                
                var ratio = random.Next(0, 100);
                
                if (ratio < nackRation)
                {
                    Logger.LogInformation($"Client -> Nacking {msg.MessageId}");
                    await subscriberClient.NackAsync(msg.MessageId);
                    return;
                }

                lock (messageStoreLock)
                {
                    var messageStr = Encoding.UTF8.GetString(msg.Data.Span);
            
                    messageStore[messageStr] -= 1;
                    
                    var receivedMessagesCount = messageStore.Values.Count(v => v <= 0);
                    var invalidMessageCount = messageStore.Values.Count(v => v < 0);

                    if (receivedMessagesCount == messageCount)
                        manualResetEvent.Set();

                    var value = messageStore[messageStr];

                    if (value < 0)
                    {
                        Logger.LogInformation($"Client invalid data for msg: {messageStr}");
                    }
                    
                    
                    if (receivedMessagesCount > shit && receivedMessagesCount % 10 == 0)
                    {
                        shit += 10;
                        messageStore[messageStr] = 1;
                        subscriberConnectionManager.SimulateConnectionDisconnection();
                        return;
                    }

                    Logger.LogInformation($"Client -> Received msg: {messageStr} id: {msg.MessageId} with valid: {receivedMessagesCount} and invalid: {invalidMessageCount}");
                }
                
                await subscriberClient.AckAsync(msg.MessageId);
   
            };

            var task = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(10000);
                    var pendingMessages = messageStore.Where(m => m.Value == 1);
                    foreach (var pendingMessage in pendingMessages)
                    {
                        Logger.LogInformation($"Client -> Pending msg is {pendingMessage.Key}");
                    }
                }
            });

            for (var i = 0; i < messageCount; i++)
            {
                // var randomString = RandomGenerator.GenerateString(10);
                var randomString = i.ToString();
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
            serviceCollection.AddSingleton<IQueueManagerStore, QueueManagerStore>();
            serviceCollection.AddTransient<IQueueManager, QueueManager>();
            serviceCollection.AddSingleton<StringPool>();
            serviceCollection.AddSingleton<MessageBrokerClient>();
            serviceCollection.AddSingleton<ISendQueueStore, SendQueueStore>();
            
            Logger.AddFileLogger(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\testo\logs.txt");
            
            return serviceCollection.BuildServiceProvider();
        }
    }
}