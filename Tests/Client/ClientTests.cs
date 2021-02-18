using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.ConnectionManager;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.QueueManagement;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
using MessageBroker.Core;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Messages.InMemoryStore;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Core.Queues;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.SessionPolicy;
using MessageBroker.Serialization;
using MessageBroker.Serialization.Pools;
using MessageBroker.Socket;
using MessageBroker.Socket.Client;
using MessageBroker.Socket.Server;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Client
{
    public class ClientTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ClientTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task TestClientSimpleScenario()
        {
            var destination = new IPEndPoint(IPAddress.Loopback, 8080);
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
            serviceCollection.AddSingleton<IClientSession, ClientSession>();
            serviceCollection.AddSingleton<ISerializer, Serializer>();
            serviceCollection.AddSingleton<IRouteMatcher, RouteMatcher>();
            serviceCollection.AddSingleton<ISocketServer, TcpSocketServer>();
            serviceCollection.AddSingleton<ISessionPolicy, RandomSessionPolicy>();
            serviceCollection.AddSingleton<IQueueStore, InMemoryQueueStore>();
            serviceCollection.AddTransient<IQueue, Queue>();
            serviceCollection.AddSingleton<Coordinator>();
            serviceCollection.AddSingleton<ISocketEventProcessor>(p => p.GetRequiredService<Coordinator>());
            serviceCollection.AddSingleton<ISocketDataProcessor>(p => p.GetRequiredService<Coordinator>());
            serviceCollection.AddSingleton<StringPool>();
            serviceCollection.AddSingleton<MessageBrokerClient>();
            serviceCollection.AddSingleton<IBinaryDataProcessor, BinaryDataProcessor>();
            serviceCollection.AddSingleton<ISendQueueStore, SendQueueStore>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var clientServiceCollection = new ServiceCollection();

            clientServiceCollection.AddSingleton<ISerializer, Serializer>();
            clientServiceCollection.AddSingleton<ISendPayloadTaskManager, SendPayloadTaskManager>();
            clientServiceCollection.AddSingleton<IConnectionManager, ConnectionManager>();
            clientServiceCollection.AddSingleton<IClientSession, ClientSession>();
            clientServiceCollection.AddSingleton<IBinaryDataProcessor, BinaryDataProcessor>();
            clientServiceCollection.AddSingleton<IReceiveDataProcessor, ReceiveDataProcessor>();
            clientServiceCollection.AddSingleton<IQueueConsumerCoordinator, QueueConsumerCoordinator>();
            clientServiceCollection.AddTransient<IQueueManager, QueueManager>();
            clientServiceCollection.AddSingleton<StringPool>();
            clientServiceCollection.AddSingleton<MessageBrokerClient>();
            clientServiceCollection.AddSingleton<ISendQueueStore, SendQueueStore>();

            var clientServiceProvider = clientServiceCollection.BuildServiceProvider();

            var server = serviceProvider.GetRequiredService<ISocketServer>();
            server.Start(destination);

            var manualResetEvent = new ManualResetEventSlim(false);
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = destination,
                RetryOnFailure = true
            };

            var client = clientServiceProvider.GetRequiredService<MessageBrokerClient>();

            client.Connect(connectionConfiguration);

            await client.ConfigureClientAsync(1000, false);

            var testQueueConsumer = client.GetQueueConsumer("TEST", "ROUTE");

            var declareResult = await testQueueConsumer.DeclareQueue();

            if (!declareResult.IsSuccess)
                throw new Exception($"declare queue failed with error {declareResult.InternalErrorCode}");

            var subscribeResult = await testQueueConsumer.SubscribeQueue();

            if (!subscribeResult.IsSuccess)
                throw new Exception($"subscription failed with error {declareResult.InternalErrorCode}");

            var l = new object();
            var currentCount = 0;

            var ackChannel = Channel.CreateUnbounded<Guid>();

            testQueueConsumer.MessageReceived += async msg =>
            {
                if (Encoding.UTF8.GetString(msg.Data.Span) == "TEST")
                {
                    lock (l)
                    {
                        currentCount += 1;
                        if (currentCount == 10000)
                        {
                            manualResetEvent.Set();
                            ackChannel.Writer.Complete();
                        }


                        Console.WriteLine($"received {currentCount} with id {msg.MessageId}");
                    }

                    await client.AckAsync(msg.MessageId);
                }
                else
                {
                    throw new Exception("Data received is not valid");
                }
            };

            for (var i = 0; i < 10000; i++)
            {
                var publishResult = await client.PublishAsync("ROUTE", Encoding.UTF8.GetBytes("TEST"));

                if (!publishResult.IsSuccess)
                    throw new Exception($"publish message failed with error {publishResult.InternalErrorCode}");
            }

            manualResetEvent.Wait();
        }
    }
}