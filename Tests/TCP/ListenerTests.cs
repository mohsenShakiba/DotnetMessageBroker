using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.TCP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Core;
using Xunit;

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

            var client = new System.Net.Sockets.TcpClient();
            
            client.Connect(endPoint);

            manualResetEvent.WaitOne(TimeSpan.FromSeconds(10));
            
            Assert.True(socketWasAccepted);
        }
    }
}