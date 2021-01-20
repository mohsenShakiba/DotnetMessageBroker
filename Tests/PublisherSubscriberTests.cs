﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Core;
using MessageBroker.Core.Persistance;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Models.Models;
using MessageBroker.Serialization;
using MessageBroker.Serialization.Pools;
using MessageBroker.SocketServer;
using Microsoft.Extensions.Logging;
using Tests.Classes;
using Xunit;

namespace Tests
{
    public class PublisherSubscriberTests
    {
        [Theory]
        [InlineData(50_000)]
        public void TestPublishSubscribe(int count)
        {
            var resetEvent = new ManualResetEvent(false);
            var publisherAck = new ManualResetEvent(false);
            var messageReceivedCount = count;

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                // builder.AddConsole();
            });

            var resolver = new SessionResolver();
            var sessionConfiguration = SessionConfiguration.Default();
            var bufferPool = new ObjectPool();
            var messageStore = new InMemoryMessageStore();
            var serializer = new Serializer();
            var dispatcher = new MessageDispatcher(resolver, serializer);
            var routeMatching = new RouteMatcher();
            var publisherEventListener = new TestEventListener();
            var subscriberEventListener = new TestEventListener();
            var coordiantor = new Coordinator(resolver, serializer, dispatcher, routeMatching, messageStore,
                loggerFactory.CreateLogger<Coordinator>());

            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

            // setup server
            var server = new TcpSocketServer(coordiantor, resolver, sessionConfiguration, loggerFactory);
            server.Start(ipEndPoint);

            // setup publisher
            var publisherSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            publisherSocket.Connect(ipEndPoint);
            var publisher = new ClientSession(publisherEventListener, publisherSocket, sessionConfiguration,
                loggerFactory.CreateLogger<ClientSession>());

            // setup subscriber
            var subscriberSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            subscriberSocket.Connect(ipEndPoint);
            var subscriber = new ClientSession(subscriberEventListener, subscriberSocket, sessionConfiguration,
                loggerFactory.CreateLogger<ClientSession>());

            // send declare queue 
            var queueDeclare = new QueueDeclare {Id = Guid.NewGuid(), Name = "TEST", Route = "TEST"};
            var queueDeclareB = serializer.ToSendPayload(queueDeclare);
            subscriber.Send(queueDeclareB.Data);

            Thread.Sleep(100);

            // send subscribe
            var subscribe = new Register {Id = Guid.NewGuid(), Concurrency = 100};
            var subscribeB = serializer.ToSendPayload(subscribe);
            subscriber.Send(subscribeB.Data);

            Thread.Sleep(100);

            // send listen
            var listen = new SubscribeQueue {Id = Guid.NewGuid(), QueueName = "TEST"};
            var listenB = serializer.ToSendPayload(listen);
            subscriber.Send(listenB.Data);

            Thread.Sleep(100);

            var receivedMessageCount = 0;
            var receivedAckCount = 0;

            Task.Factory.StartNew(() =>
            {
                subscriberEventListener.ReceivedEvent += (_, d) =>
                {
                    var payloadType = serializer.ParsePayloadType(d);

                    switch (payloadType)
                    {
                        case PayloadType.Msg:
                            var receivedMessage = serializer.ToMessage(d);
                            Interlocked.Increment(ref receivedMessageCount);
                            if (receivedMessageCount == count)
                                resetEvent.Set();

                            var ack = new Ack {Id = receivedMessage.Id};
                            var ackB = serializer.ToSendPayload(ack);
                            subscriber.Send(ackB.Data);

                            break;
                        case PayloadType.Ack:
                            break;
                        default:
                            throw new Exception("test");
                    }
                };
            });

            Task.Factory.StartNew(() =>
            {
                publisherEventListener.ReceivedEvent += (_, d) =>
                {
                    var payloadType = serializer.ParsePayloadType(d);
                    switch (payloadType)
                    {
                        case PayloadType.Ack:
                            var receivedMessage = serializer.ToAck(d);
                            Interlocked.Increment(ref receivedAckCount);
                            if (receivedAckCount == count)
                                publisherAck.Set();
                            break;
                        default:
                            Console.WriteLine("incorrect msg");
                            break;
                    }
                };
            });

            Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < count; i++)
                {
                    var guid = Guid.NewGuid();
                    var message = new Message {Id = guid, Route = "TEST", Data = Encoding.UTF8.GetBytes("TEST")};
                    var messageB = serializer.ToSendPayload(message);
                    publisher.Send(messageB.Data);
                    bufferPool.Return(messageB);
                }
            });

            while (receivedMessageCount != count)
            {
                Console.WriteLine("data is " + coordiantor._stat);
                Thread.Sleep(1000);
            }

            ;

            resetEvent.WaitOne();
            publisherAck.WaitOne();

            server.Stop();

            Console.WriteLine("done");
        }
    }
}