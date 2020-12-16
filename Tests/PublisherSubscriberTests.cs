using MessageBroker.Common;
using MessageBroker.Core;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        public PublisherSubscriberTests()
        {

        }

        [Theory]
        [InlineData(100)]
        public void TestPublishSubscribe(int count)
        {
            var resetEvent = new ManualResetEvent(false);
            var publisherAck = new ManualResetEvent(false);
            var messageReceivedCount = count;

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var messageProcessor = new MessageProcessor();
            var resolver = new SessionResolver();
            var sessionConfiguration = SessionConfiguration.Default();
            var parser = new Parser();
            var dispatcher = new MessageDispatcher(resolver);
            var routeMatching = new DefaultRouteMatching();
            var publisherEventListener = new TestEventListener();
            var subscriberEventListener = new TestEventListener();

            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

            // setup server
            var server = new TcpSocketServer(messageProcessor, resolver, sessionConfiguration, loggerFactory);
            server.Start(ipEndPoint);

            // setup publisher
            var publisherSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            publisherSocket.Connect(ipEndPoint);
            var publisher = new ClientSession(publisherEventListener, publisherSocket, sessionConfiguration, loggerFactory.CreateLogger<ClientSession>());

            // setup subscriber
            var subscriberSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            subscriberSocket.Connect(ipEndPoint);
            var subscriber = new ClientSession(subscriberEventListener, subscriberSocket, sessionConfiguration, loggerFactory.CreateLogger<ClientSession>());

            var coordiantor = new Coordinator(messageProcessor, resolver, parser, dispatcher, routeMatching, loggerFactory.CreateLogger<Coordinator>());

            // send listen
            var listen = new Listen("TEST");
            var listenB = parser.ToBinary(listen);
            subscriber.SendSync(listenB);

            var receivedMessageCount = 0;
            var receivedAckCount = 0;

            Task.Factory.StartNew(() =>
            {
                subscriberEventListener.ReceivedEvent += (_, d) =>
                {
                    var receivedMessage = parser.Parse(d.Span);
                    switch (receivedMessage)
                    {
                        case Message m:
                            Interlocked.Increment(ref receivedMessageCount);
                            if (receivedMessageCount == count)
                                resetEvent.Set();

                            var ack = new Ack(m.Id);
                            var ackB = parser.ToBinary(ack);
                            subscriber.SendSync(ackB);
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
                    var receivedMessage = parser.Parse(d.Span);
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
                    var messageB = parser.ToBinary(message);
                    publisher.SendSync(messageB);
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
