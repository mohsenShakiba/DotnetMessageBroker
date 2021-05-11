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
        [InlineData(20000)]
        public async Task Test(int count)
        {
            var topicName = RandomGenerator.GenerateString(10);
            var wait = new ManualResetEvent(false);
            var serverIpEndpoint = new IPEndPoint(IPAddress.Loopback, 8000);

            var clientConnectionConfiguration = new ClientConnectionConfiguration
            {
                AutoReconnect = true,
                IpEndPoint = serverIpEndpoint
            };
            
            var brokerBuilder = new BrokerBuilder();

            using var broker = brokerBuilder
                .UseMemoryStore()
                .UseEndPoint(serverIpEndpoint)
                .AddConsoleLog()
                .Build();
            
            
            broker.Start();

            await using var clientFactory = new BrokerClientFactory();

            var client = clientFactory.GetClient();
            
            client.Connect(clientConnectionConfiguration);
            
            var result = await client.DeclareTopicAsync(topicName, topicName);
            
            Assert.True(result.IsSuccess);
            
            var subscription = await client.GetTopicSubscriptionAsync(topicName, topicName);
            
            var numberOfReceivedMessages = 0;

            subscription.MessageReceived += message =>
            {
                if (Encoding.UTF8.GetString(message.Data.Span) != numberOfReceivedMessages.ToString())
                {
                    Console.WriteLine($"missed number {numberOfReceivedMessages}");
                }
                Interlocked.Increment(ref numberOfReceivedMessages);
                
                Logger.LogInformation($"received message {Encoding.UTF8.GetString(message.Data.Span)} with id {message.MessageId}");
                message.Ack();

                if (numberOfReceivedMessages == count)
                {
                    wait.Set();
                }
            };

            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < count; i++)
            {
                var randomString = i.ToString();
                var randomData = Encoding.UTF8.GetBytes(randomString);
                var response = await client.PublishAsync(topicName, randomData);

                if (!response.IsSuccess)
                    throw new Exception("Failed to send data to server");
            }
            
            sw.Stop();
            
            Console.WriteLine($"took {sw.ElapsedMilliseconds}");


            await Task.Delay(1000);
            
            var lastCheck = 0;
            
            while (true)
            {
                if (count == numberOfReceivedMessages)
                {
                    break;
                }

                if (lastCheck != numberOfReceivedMessages)
                {
                    lastCheck = numberOfReceivedMessages;
                    await Task.Delay(100);
                }
                else
                {
                    break;
                }

            }
            
            Assert.Equal(count, numberOfReceivedMessages);

        }
        
        // [Theory]
        // [InlineData(10000, 0)]
        // public async Task EndToEndTest_SingleSubscriberSinglePublisherNoInterrupt_AllMessagesAreReceivedBySubscriber(
        //     int messageCount, int nackRation)
        // {
        //     // declare variables
        //     var messageStore = new Dictionary<string, int>();
        //     var random = new Random();
        //     var messageStoreLock = new object();
        //     var queueName = RandomGenerator.GenerateString(10);
        //     var queueRoute = RandomGenerator.GenerateString(10);
        //     var destination = new IPEndPoint(IPAddress.Loopback, 8000);
        //     var serverServiceProvider = GetServerServiceProvider();
        //     var subscriberServiceProvider = GetClientServiceProvider();
        //     var publisherServiceProvider = GetClientServiceProvider();
        //
        //     // setup server
        //     var server = serverServiceProvider.GetRequiredService<ISocketServer>();
        //     server.Start(destination);
        //
        //     // setup send client
        //     var subscriberClient = subscriberServiceProvider.GetRequiredService<MessageBrokerClient>();
        //     subscriberClient.Connect(destination);
        //
        //     // setup receive client
        //     var publisherClient = publisherServiceProvider.GetRequiredService<MessageBrokerClient>();
        //     publisherClient.Connect(destination);
        //
        //     // declare the queue
        //     var declareResult = await subscriberClient.CreateQueueAsync(queueName, queueRoute);
        //
        //     if (!declareResult.IsSuccess)
        //         throw new Exception($"declare queue failed with error {declareResult.InternalErrorCode}");
        //     
        //     // setup reset event
        //     var manualResetEvent = new ManualResetEventSlim(false);
        //
        //     // setup queue 
        //     var queueManager = await subscriberClient.GetQueueSubscriber(queueName, queueRoute, 100);
        //
        //     // setup subscriber
        //     queueManager.MessageReceived += async msg =>
        //     {
        //         var ratio = random.Next(0, 100);
        //         
        //         var messageStr = Encoding.UTF8.GetString(msg.Data.Span);
        //
        //         if (ratio < nackRation)
        //         {
        //             Logger.LogInformation($"nacked message {messageStr}");
        //             var res = await subscriberClient.NackAsync(msg.MessageId);
        //             Logger.LogInformation($"nacked message {messageStr} after {res.IsSuccess}");
        //             return;
        //         }
        //
        //         lock (messageStoreLock)
        //         {
        //
        //             if (messageStore.ContainsKey(messageStr))
        //             {
        //                 messageStore[messageStr] -= 1;
        //             }
        //
        //             var receivedMessagesCount = messageStore.Values.Count(v => v == 0);
        //
        //             if (receivedMessagesCount == messageCount)
        //                 manualResetEvent.Set();
        //             
        //             Logger.LogInformation($"received message {messageStr} {receivedMessagesCount}");
        //         }
        //         
        //         await subscriberClient.AckAsync(msg.MessageId);
        //     };
        //     
        //     for (var i = 0; i < messageCount; i++)
        //     {
        //
        //         
        //         var randomString = i.ToString();
        //         var randomData = Encoding.UTF8.GetBytes(randomString);
        //
        //         lock (messageStoreLock)
        //         {
        //             if (messageStore.ContainsKey(randomString))
        //                 messageStore[randomString] += 1;
        //             else
        //                 messageStore[randomString] = 1;
        //         }
        //
        //         var publishResult = await publisherClient.PublishAsync(queueRoute, randomData);
        //
        //         if (!publishResult.IsSuccess)
        //             throw new Exception($"publish message failed with error {publishResult.InternalErrorCode}");
        //         
        //     }
        //
        //     manualResetEvent.Wait();
        //     server.Stop();
        // }


        [Theory]
        [InlineData(100, 30, 15)]
        public async Task EndToEndTest_SingleSubscriberSinglePublisherWithInterrupts_AllMessagesAreReceivedBySubscriber(int messageCount, int nackRation, int failureRatio)
        {
            // declare variables
            var queueName = RandomGenerator.GenerateString(10);
            var messageStore = new MessageStore(queueName, messageCount);
            var random = new Random();
            var serverIpEndpoint = new IPEndPoint(IPAddress.Loopback, 8001);
            var clientConnectionConfiguration = new ClientConnectionConfiguration
            {
                AutoReconnect = true,
                IpEndPoint = serverIpEndpoint
            };
            
            // setup server
            var brokerBuilder = new BrokerBuilder();

            using var broker = brokerBuilder
                .UseMemoryStore()
                .UseEndPoint(serverIpEndpoint)
                // .AddConsoleLog()
                .AddFile(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\Logs\test.txt")
                .Build();
            
            broker.Start();
        
            await using var clientFactory = new BrokerClientFactory();
            
            // setup subscriber
            var subscriberClient = clientFactory.GetClient();
            subscriberClient.Connect(clientConnectionConfiguration, true);
        
            // setup publisher
            var publisherClient = clientFactory.GetClient();
            publisherClient.Connect(clientConnectionConfiguration);
        
            // declare topic
            var declareResult = await subscriberClient.DeclareTopicAsync(queueName, queueName);
            Assert.True(declareResult.IsSuccess);
        
            // setup queue 
            var subscription = await subscriberClient.GetTopicSubscriptionAsync(queueName, queueName);
            
            // // setup subscriber
            // subscription.MessageReceived += msg =>
            // {
            //     try
            //     {
            //         Logger.LogInformation($"received message in client with id: {msg.MessageId} {messageStore.ReceivedCount}");
            //         var ratio = random.Next(0, 100);
            //     
            //         if (ratio < failureRatio)
            //         {
            //             // intentionally interrupt the socket
            //             subscriberClient.ConnectionManager.Socket.SimulateInterrupt();
            //             return;
            //         }
            //
            //         if (ratio < nackRation)
            //         {
            //             msg.Nack();
            //             return;
            //         }
            //
            //         var messageData = new Guid(msg.Data.Span);
            //
            //         messageStore.OnMessageReceived(messageData);
            //     
            //     
            //         msg.Ack();
            //     }
            //     catch (Exception e)
            //     {
            //         Console.WriteLine(e);
            //         throw;
            //     }
            //     
            // };

            publisherClient.ConnectionManager.ReceiveDataProcessor.OnOkReceived += (guid) =>
            {
                messageStore.OnOkReceived(guid);
            };

            while (messageStore.CurrentCount < messageCount)
            {
                if (random.Next(0, 100) < failureRatio)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        publisherClient.ConnectionManager.Socket.SimulateInterrupt();
                    });
                }

                var msg = messageStore.GetUniqueMessage();

                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                
                var publishResult = await publisherClient.PublishRawAsync(msg, cancellationTokenSource.Token);
                
                Logger.LogInformation($"client sent message with id {msg.Id} and count {messageStore.CurrentCount}");
        
                if (!publishResult.IsSuccess)
                {
                    // throw new Exception($"Sending failed, reason: {publishResult.InternalErrorCode}");
                }
                else
                {
                    messageStore.Commit(msg.Id, msg);
                }

            }

            await Task.Delay(1000);

            messageStore.WaitForSendDataToFinish();
            

            // messageStore.WaitForDataToFinish();
            // Assert.Equal(messageCount, messageStore.ReceivedCount);   
        }

       
    }
}