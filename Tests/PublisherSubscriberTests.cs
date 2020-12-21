using MessageBroker.Core;
using MessageBroker.Core.BufferPool;
using MessageBroker.Core.MessageRefStore;
using MessageBroker.Core.Models;
using MessageBroker.Core.Persistance;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.Serialize;
using MessageBroker.SocketServer;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tests.Classes;
using Xunit;

namespace Tests
{
    public class PublisherSubscriberTests
    {
        [Theory]
        [InlineData(100_000)]
        public void TestPublishSubscribe(int count)
        {
            var resetEvent = new ManualResetEvent(false);
            var publisherAck = new ManualResetEvent(false);
            var messageReceivedCount = count;

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var resolver = new SessionResolver();
            var sessionConfiguration = SessionConfiguration.Default();
            var messageRefStore = new DefaultMessageRefStore();
            var bufferPool = new DefaultBufferPool();
            var messageStore = new InMemoryMessageStore();
            var serializer = new DefaultSerializer(bufferPool);
            var dispatcher = new MessageDispatcher(resolver, serializer, messageRefStore);
            var routeMatching = new DefaultRouteMatching();
            var publisherEventListener = new TestEventListener();
            var subscriberEventListener = new TestEventListener();
            var coordiantor = new Coordinator(resolver, serializer, dispatcher, routeMatching, messageStore, messageRefStore, loggerFactory.CreateLogger<Coordinator>());

            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

            // setup server
            var server = new TcpSocketServer(coordiantor, resolver, sessionConfiguration, loggerFactory);
            server.Start(ipEndPoint);

            // setup publisher
            var publisherSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            publisherSocket.Connect(ipEndPoint);
            var publisher = new ClientSession(publisherEventListener, publisherSocket, sessionConfiguration, loggerFactory.CreateLogger<ClientSession>());

            // setup subscriber
            var subscriberSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            subscriberSocket.Connect(ipEndPoint);
            var subscriber = new ClientSession(subscriberEventListener, subscriberSocket, sessionConfiguration, loggerFactory.CreateLogger<ClientSession>());

            // send subscribe
            var subscribe = new Subscribe { Id = Guid.NewGuid(), Concurrency = 10 };
            var subscribeB = serializer.Serialize(subscribe);
            subscriber.Send(subscribeB);

            Thread.Sleep(1000);

            // send listen
            var listen = new Listen(Guid.NewGuid(), "TEST");
            var listenB = serializer.Serialize(listen);
            subscriber.Send(listenB);

            Thread.Sleep(1000);

            var receivedMessageCount = 0;
            var receivedAckCount = 0;

            Task.Factory.StartNew(() =>
            {
                subscriberEventListener.ReceivedEvent += (_, d) =>
                {
                    var receivedMessage = serializer.Deserialize(d);
                    switch (receivedMessage)
                    {
                        case Message m:
                            Interlocked.Increment(ref receivedMessageCount);
                            if (receivedMessageCount == count)
                                resetEvent.Set();

                            var ack = new Ack(m.Id);
                            var ackB = serializer.Serialize(ack);
                            subscriber.Send(ackB);
                            break;
                        case Ack a:
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
                    var receivedMessage = serializer.Deserialize(d);
                    switch (receivedMessage)
                    {
                        case Ack a:
                            Interlocked.Increment(ref receivedAckCount);
                            if (receivedAckCount == count)
                                publisherAck.Set();
                            break;
                        default:
                            throw new Exception("test");
                    }
                };
            });

            Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < count; i++)
                {
                var guid = Guid.NewGuid();
                var delimiter = Encoding.UTF8.GetBytes("\n").First();

                while (true)
                {
                    var s = guid.ToByteArray().AsSpan();
                    if (s.Contains(delimiter))
                    {
                        guid = Guid.NewGuid();
                    }
                    else
                    {
                        break;
                    }

                }

                var message = new Message(guid, "TEST", Encoding.UTF8.GetBytes("TEST"));
                    var messageB = serializer.Serialize(message);
                    publisher.Send(messageB);
                }
            });



            while (receivedMessageCount != count)
            {
                Console.WriteLine("data is " + coordiantor._stat);
                Thread.Sleep(1000);
            };

            resetEvent.WaitOne();
            publisherAck.WaitOne();

            Console.WriteLine("done");
        }
    }
}
