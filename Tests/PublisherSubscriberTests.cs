// using System;
// using System.Net;
// using System.Net.Sockets;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using MessageBroker.Common.Pooling;
// using MessageBroker.Core;
// using MessageBroker.Core.Persistence;
// using MessageBroker.Core.Persistence.InMemoryStore;
// using MessageBroker.Core.Queues;
// using MessageBroker.Core.RouteMatching;
// using MessageBroker.Core.SessionPolicy;
// using MessageBroker.Core.Socket;
// using MessageBroker.Core.Socket.Client;
// using MessageBroker.Core.Socket.Server;
// using MessageBroker.Core.StatRecording;
// using MessageBroker.Models;
// using MessageBroker.Serialization;
// using MessageBroker.Serialization.Pools;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Tests.Classes;
// using Xunit;
//
// namespace Tests
// {
//     public class PublisherSubscriberTests
//     {
//         [Theory]
//         [InlineData(50000)]
//         public void TestPublishSubscribe(int count)
//         {
//             var resetEvent = new ManualResetEvent(false);
//             var publisherAck = new ManualResetEvent(false);
//
//
//             var serviceCollection = new ServiceCollection();
//             
//             serviceCollection.AddSingleton<ISessionResolver, SessionResolver>();
//             serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
//             serviceCollection.AddSingleton<ISerializer, Serializer>();
//             serviceCollection.AddSingleton<IRouteMatcher, RouteMatcher>();
//             serviceCollection.AddSingleton<IStatRecorder, StatRecorder>();
//             serviceCollection.AddSingleton<ISocketServer, TcpSocketServer>();
//             serviceCollection.AddSingleton<ISessionPolicy, RandomSessionPolicy>();
//             serviceCollection.AddSingleton<IQueueStore, QueueStore>();
//             serviceCollection.AddTransient<IQueue, Queue>();
//             serviceCollection.AddSingleton<ISocketEventProcessor, Coordinator>();
//             serviceCollection.AddSingleton<StringPool>();
//             serviceCollection.AddSingleton<MessageDispatcher>();
//             serviceCollection.AddSingleton(_ =>
//             {
//                 return LoggerFactory.Create(b => {});
//             });
//
//             var serviceProvider = serviceCollection.BuildServiceProvider();
//
//             var serializer = serviceProvider.GetRequiredService<ISerializer>();
//             var statRecorder = serviceProvider.GetRequiredService<IStatRecorder>();
//             var publisherEventListener = new TestEventListener();
//             var subscriberEventListener = new TestEventListener();
//
//             var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);
//
//             // setup server
//             var server = serviceProvider.GetRequiredService<ISocketServer>();
//             server.Start(ipEndPoint);
//
//             // setup publisher
//             var publisherSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//             publisherSocket.Connect(ipEndPoint);
//             var publisher = new ClientSession(publisherEventListener, publisherSocket);
//
//             // setup subscriber
//             var subscriberSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//             subscriberSocket.Connect(ipEndPoint);
//             var subscriber = new ClientSession(subscriberEventListener, subscriberSocket);
//             
//             // send declare queue 
//             var queueDeclare = new QueueDeclare {Id = Guid.NewGuid(), Name = "TEST", Route = "TEST"};
//             var queueDeclareB = serializer.ToSendPayload(queueDeclare);
//             subscriber.Send(queueDeclareB.Data);
//
//             Thread.Sleep(100);
//
//             // send listen
//             var listen = new SubscribeQueue {Id = Guid.NewGuid(), QueueName = "TEST"};
//             var listenB = serializer.ToSendPayload(listen);
//             subscriber.Send(listenB.Data);
//             
//             // send configure
//             var configureSubscriber = new ConfigureSubscription {AutoAck = false, Concurrency = 1000};
//             var configureSubscriptionB = serializer.ToSendPayload(configureSubscriber);
//             subscriber.Send(configureSubscriptionB.Data);
//
//             Thread.Sleep(100);
//
//             var receivedMessageCount = 0;
//             var receivedAckCount = 0;
//
//             subscriberEventListener.ReceivedEvent += (_, d) =>
//             {
//                 var payloadType = serializer.ParsePayloadType(d);
//
//                 switch (payloadType)
//                 {
//                     case PayloadType.Msg:
//                         var receivedMessage = serializer.ToQueueMessage(d);
//                         Interlocked.Increment(ref receivedMessageCount);
//                         if (receivedMessageCount == count)
//                             resetEvent.Set();
//
//                         var ack = new Ack {Id = receivedMessage.Id};
//                         var ackB = serializer.ToSendPayload(ack);
//                         subscriber.Send(ackB.Data);
//                         ObjectPool.Shared.Return(ackB);
//
//                         break;
//                     case PayloadType.Ack:
//                         break;
//                     default:
//                         throw new Exception("test");
//                 }
//             };
//
//             Task.Factory.StartNew(() =>
//             {
//                 publisherEventListener.ReceivedEvent += (_, d) =>
//                 {
//                     var payloadType = serializer.ParsePayloadType(d);
//                     switch (payloadType)
//                     {
//                         case PayloadType.Ack:
//                             var receivedMessage = serializer.ToAck(d);
//                             Interlocked.Increment(ref receivedAckCount);
//                             if (receivedAckCount == count)
//                                 publisherAck.Set();
//                             break;
//                         default:
//                             Console.WriteLine("incorrect msg");
//                             break;
//                     }
//                 };
//             });
//
//             Task.Factory.StartNew(() =>
//             {
//                 for (var i = 0; i < count; i++)
//                 {
//                     try
//                     {
//                         var guid = Guid.NewGuid();
//                         var message = new Message {Id = guid, Route = "TEST", Data = Encoding.UTF8.GetBytes("TEST")};
//                         var messageB = serializer.ToSendPayload(message);
//                         publisher.Send(messageB.Data);
//                         ObjectPool.Shared.Return(messageB);
//                     }
//                     catch (Exception e)
//                     {
//                         Console.WriteLine(e);
//                         throw;
//                     }
//                 }
//
//             });
//
//             while (receivedMessageCount != count)
//             {
//                 Console.WriteLine("data is " + statRecorder.MessageReceived + " rec: " + receivedMessageCount);
//                 Thread.Sleep(1000);
//             }
//             
//
//             resetEvent.WaitOne();
//             publisherAck.WaitOne();
//             
//
//             server.Stop();
//
//             Console.WriteLine("done");
//         }
//     }
// }

