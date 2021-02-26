using System;
using System.Collections.Generic;
using System.IO;
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
        public async Task TestClientSimpleScenario(int messageCount, int nackRation)
        {
            
            // declare variables
            var messageStore = new Dictionary<string, int>();
            var messageStoreLock = new object();
            var random = new Random();
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
            subscriberClient.Connect(connectionConfiguration, true);
            
            // setup receive client
            var publisherClient = clientServiceProvider2.GetRequiredService<MessageBrokerClient>();
            publisherClient.Connect(connectionConfiguration, false);

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
                
                // var ratio = random.Next(0, 100);
                //
                // if (ratio < nackRation)
                // {
                //     await client.NackAsync(msg.MessageId);
                //     return;
                // }
                
                try
                {
                    lock (messageStoreLock)
                    {
                        var messageStr = Encoding.UTF8.GetString(msg.Data.Span);
                
                        if (messageStore.ContainsKey(messageStr))
                        {
                            messageStore[messageStr] -= 1;
                        }
                        
                        var receivedMessagesCount = messageStore.Values.Count(v => v == 0);
                
                        using(var sw = File.AppendText(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\testo\test_client.txt"))
                        {
                            sw.WriteLine($"received message in client count is {receivedMessagesCount} {messageStr}");
                        }
                
                        if (receivedMessagesCount == messageCount)
                            manualResetEvent.Set();
                    }
                    
                    await subscriberClient.AckAsync(msg.MessageId);
                }
                catch (Exception e)
                {
                    using(var sw = File.AppendText(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\testo\error.txt"))
                    {
                        sw.WriteLine($"error was received");
                    }
                    throw;
                }
            };

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
        }

        [Fact]
        public void EndToEndTest_SingleSubscriberSinglePublisherNoInterrupt_AllMessagesAreReceivedBySubscriber()
        {
            
        }
        // EndToEndTest_SingleSubscriberSinglePublisherWithInterrupts_AllMessagesAreReceivedBySubscriber
        // EndToEndTest_MultipleSubscribersMultiplePublishersNoInterrupt_AllMessagesAreReceivedBuSubscriber
        // EndToEndTest_MultipleSubscribersMultiplePublishersWithInterrupts_AllMessagesAreReceivedBuSubscriber

        private IServiceProvider GetServerServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IPayloadProcessor, PayloadProcessor>();
            serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
            serviceCollection.AddSingleton<IClientSession, ClientSession>();
            serviceCollection.AddSingleton<ISerializer, Serializer>();
            serviceCollection.AddSingleton<IRouteMatcher, RouteMatcher>();
            serviceCollection.AddSingleton<ISocketServer, TcpSocketServer>();
            serviceCollection.AddSingleton<ISessionPolicy, RoundRobinSessionPolicy>();
            serviceCollection.AddSingleton<IQueueStore, InMemoryQueueStore>();
            serviceCollection.AddTransient<IQueue, Queue>();
            serviceCollection.AddSingleton<Coordinator>();
            serviceCollection.AddSingleton<ISocketEventProcessor>(p => p.GetRequiredService<Coordinator>());
            serviceCollection.AddSingleton<ISocketDataProcessor>(p => p.GetRequiredService<Coordinator>());
            serviceCollection.AddSingleton<StringPool>();
            serviceCollection.AddSingleton<MessageBrokerClient>();
            serviceCollection.AddSingleton<IBinaryDataProcessor, BinaryDataProcessor>();
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
            
            Logger.AddFileLogger();
            
            return serviceCollection.BuildServiceProvider();
        }
    }
}