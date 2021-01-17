using MessageBroker.Core.BufferPool;
using MessageBroker.Core.Models;
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
    public class ServerClientTests
    {
        private Random random = new Random();

        [Theory]
        [InlineData(1_000, 100)]
        [InlineData(10_000, 100)]
        [InlineData(100_000, 100)]
        public void ServerClientReceive(int count, int msgSize)
        {
            var resetEvent = new ManualResetEvent(false);
            var messageReceivedCount = count;

            var serializer = new DefaultSerializer();
            var loggerFactory = new LoggerFactory();
            var messageProcessor = new TestMessageProcessor();
            var resolver = new SessionResolver();
            var sessionConfiguration = SessionConfiguration.Default();

            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

            var server = new TcpSocketServer(messageProcessor, resolver, sessionConfiguration, loggerFactory);
            server.Start(ipEndPoint);

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ipEndPoint);

            Thread.Sleep(100);

            var messagePayload = RandomString(msgSize);
            var message = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = Encoding.UTF8.GetBytes(messagePayload)
            };

            var payload = serializer.ToSendPayload(message);

            messageProcessor.OnDataReceived += (_, msg) =>
            {
                messageReceivedCount -= 1;

                if (messageReceivedCount == 0)
                    resetEvent.Set();
            };

            for (var i = 0; i < count; i++)
            {
                client.Send(BitConverter.GetBytes(payload.Data.Length));
                client.Send(payload.Data.ToArray());
            }

            resetEvent.WaitOne();

            server.Stop();
        }
        
        [Theory]
        [InlineData(1_000, 100)]
        [InlineData(10_000, 100)]
        [InlineData(100_000, 100)]
        public void ServerClientSend(int count, int msgSize)
        {
            var resetEvent = new AutoResetEvent(false);
            var messageReceivedCount = count;
            Guid sessionId = new();

            var serializer = new DefaultSerializer();
            var loggerFactory = new LoggerFactory();
            var messageProcessor = new TestMessageProcessor();
            var resolver = new SessionResolver();
            var sessionConfiguration = SessionConfiguration.Default();
            var eventListener = new TestEventListener();

            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

            var server = new TcpSocketServer(messageProcessor, resolver, sessionConfiguration, loggerFactory);
            server.Start(ipEndPoint);


            messageProcessor.OnClientConnected += (sid) =>
            {
                sessionId = sid;
                resetEvent.Set();
            };

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ipEndPoint);

            Thread.Sleep(100);

            var messagePayload = RandomString(msgSize);
            var message = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = Encoding.UTF8.GetBytes(messagePayload)
            };

            var payload = serializer.ToSendPayload(message);

            var data = payload.Data;

            Task.Factory.StartNew(() =>
            {
                var buffer = new byte[data.Length];

                for(var i = 0; i < count; i++)
                {
                    var len = client.Receive(buffer);

                    var msg = serializer.ToMessage(buffer.AsMemory(4));

                    Assert.Equal(messagePayload, Encoding.UTF8.GetString(msg.Data.Slice(0, msgSize).Span));
                }

                resetEvent.Set();

            });

            resetEvent.WaitOne();

            var clientSession = resolver.Sessions.First();

            for (var i = 0; i < count; i++)
            {
                clientSession.Send(payload.Data);
            }

            resetEvent.WaitOne();

            server.Stop();
        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
