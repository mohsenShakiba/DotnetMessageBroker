using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TcpListener = MessageBroker.Common.Tcp.TcpListener;

namespace Tests.TCP
{
    public class ListenerTests
    {
        [Fact]
        public void ListenForIncomingConnection_WithValidClient_AcceptConnectionAndNotify()
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, 8100);
            var listener = new TcpListener(endPoint, NullLogger<TcpListener>.Instance);
            listener.Start();

            var socketWasAccepted = false;
            var manualResetEvent = new ManualResetEvent(false);

            listener.OnSocketAccepted += (_, args) =>
            {
                manualResetEvent.Set();
                socketWasAccepted = true;
            };

            var client = new TcpClient();

            client.Connect(endPoint);

            manualResetEvent.WaitOne(TimeSpan.FromSeconds(10));

            Assert.True(socketWasAccepted);
        }
    }
}