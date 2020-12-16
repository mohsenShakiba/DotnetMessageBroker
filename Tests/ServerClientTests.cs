using MessageBroker.Common;
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

            var loggerFactory = new LoggerFactory();
            var messageProcessor = new MessageProcessor();
            var resolver = new SessionResolver();
            var sessionConfiguration = SessionConfiguration.Default();

            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

            var server = new TcpSocketServer(messageProcessor, resolver, sessionConfiguration, loggerFactory);
            server.Start(ipEndPoint);

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ipEndPoint);

            Thread.Sleep(100);

            var message = RandomString(msgSize);

            messageProcessor.OnMessageReceived += (_, msg) =>
            {
                messageReceivedCount -= 1;

                if (messageReceivedCount == 0)
                    resetEvent.Set();
            };

            var payload = MessageToByte(message);

            for (var i = 0; i < count; i++)
            {
                client.Send(payload);
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

            var loggerFactory = new LoggerFactory();
            var messageProcessor = new MessageProcessor();
            var resolver = new SessionResolver();
            var sessionConfiguration = SessionConfiguration.Default();

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

            var message = RandomString(msgSize);

            var payload = MessageToByte(message).ToArray();

            Task.Factory.StartNew(() =>
            {
                var buffer = new byte[payload.Length];

                for(var i = 0; i < count; i++)
                {
                    var len = client.Receive(buffer);

                    Assert.Equal(message, Encoding.ASCII.GetString(buffer.AsSpan(4, msgSize)));
                }

                resetEvent.Set();

            });

            resetEvent.WaitOne();


            for (var i = 0; i < count; i++)
            {
                server.Send(sessionId, payload);
            }

            resetEvent.WaitOne();

            server.Stop();
        }

        private Span<byte> MessageToByte(string message)
        {
            var len = BitConverter.GetBytes(message.Length);
            var msg = Encoding.ASCII.GetBytes(message);

            var payload = new byte[len.Length + msg.Length];

            len.CopyTo(payload, 0);
            msg.CopyTo(payload, len.Length);

            return payload;
        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
