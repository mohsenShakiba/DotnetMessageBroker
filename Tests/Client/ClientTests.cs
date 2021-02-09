using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.Models;
using MessageBroker.Core;
using MessageBroker.Core.Persistence;
using MessageBroker.Core.Persistence.InMemoryStore;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Core.Queues;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.SessionPolicy;
using MessageBroker.Core.Socket;
using MessageBroker.Core.Socket.Server;
using MessageBroker.Serialization;
using MessageBroker.Serialization.Pools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tests.Client
{
    public class ClientTests
    {
        
        [Fact]
        public async Task TestClientSimpleScenario()
        {
            
            var destination = new IPEndPoint(IPAddress.Loopback, 8080);
            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
            serviceCollection.AddSingleton<ISerializer, Serializer>();
            serviceCollection.AddSingleton<IRouteMatcher, RouteMatcher>();
            serviceCollection.AddSingleton<ISocketServer, TcpSocketServer>();
            serviceCollection.AddSingleton<ISessionPolicy, RandomSessionPolicy>();
            serviceCollection.AddSingleton<IQueueStore, InMemoryQueueStore>();
            serviceCollection.AddTransient<IQueue, Queue>();
            serviceCollection.AddSingleton<ISocketEventProcessor, Coordinator>();
            serviceCollection.AddSingleton<StringPool>();
            serviceCollection.AddSingleton<MessageDispatcher>();
            serviceCollection.AddSingleton(_ =>
            {
                return LoggerFactory.Create(b => {});
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var server = serviceProvider.GetRequiredService<ISocketServer>();
            server.Start(destination);
                
            var manualResetEvent = new ManualResetEventSlim(false);
            var connectionConfiguration = new SocketConnectionConfiguration
            {
                IpEndPoint = destination,
                RetryOnFailure = true
            };
            var client = new MessageBrokerClient();
            
            client.Connect(connectionConfiguration);

            await client.ConfigureClientAsync(10, false);

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
            
            testQueueConsumer.MessageReceived += async (msg) =>
            {
                if (Encoding.UTF8.GetString(msg.Data.Span) == "TEST")
                {
                    lock (l)
                    {
                        currentCount += 1;
                        if (currentCount == 100000)
                        {
                            manualResetEvent.Set();
                            ackChannel.Writer.Complete();
                        }
                    }

                    await client.AckAsync(msg.MessageId);
                }
                else
                {
                    throw new Exception("Data received is not valid");
                }
            };

            for (var i = 0; i < 100000; i++)
            {
                var publishResult = await client.PublishAsync("ROUTE", Encoding.UTF8.GetBytes("TEST"));

                if (!publishResult.IsSuccess)
                    throw new Exception($"publish message failed with error {publishResult.InternalErrorCode}");
            }

            manualResetEvent.Wait();
        }
    }
}