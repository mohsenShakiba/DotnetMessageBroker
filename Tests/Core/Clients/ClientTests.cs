using System;
using System.Threading;
using System.Threading.Channels;
using MessageBroker.TCP;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Core.Clients
{
    public class ClientTests
    {
        [Fact]
        public void Enqueue_NotDisposed_SocketSendAsyncIsCalled()
        {
            var socket = new Mock<ISocket>();

            var serializedPayload = RandomGenerator.GetMessageSerializedPayload();

            socket.Setup(s => s.Connected)
                .Returns(true);
            
            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
                .ReturnsAsync(serializedPayload.Data.Length);
            
            var client = new MessageBroker.Core.Clients.Client(socket.Object,
                NullLogger<MessageBroker.Core.Clients.Client>.Instance);


            client.Enqueue(serializedPayload);

            client.SendNextMessageInQueue();
            
            socket.Verify(s => s.SendAsync(serializedPayload.Data, CancellationToken.None));
        }

        [Fact]
        public void OnPayloadAckReceived_AnyCondition_StatusIsSetForTicket()
        {
            var socket = new Mock<ISocket>();

            var serializedPayload = RandomGenerator.GetMessageSerializedPayload();

            socket.Setup(s => s.Connected)
                .Returns(true);
            
            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
                .ReturnsAsync(serializedPayload.Data.Length);
            
            var client = new MessageBroker.Core.Clients.Client(socket.Object,
                NullLogger<MessageBroker.Core.Clients.Client>.Instance);

            var ticket = client.Enqueue(serializedPayload);

            var didReceiveAck = false;

            ticket.OnStatusChanged += (guid, b) =>
            {
                if (b)
                {
                    didReceiveAck = true;
                }
            };

            client.SendNextMessageInQueue();
            
            client.OnPayloadAckReceived(serializedPayload.PayloadId);

            Thread.Yield();
            
            Assert.True(didReceiveAck);

        }
        
        [Fact]
        public void OnPayloadNackReceived_AnyCondition_StatusIsSetForTicket()
        {
            var socket = new Mock<ISocket>();

            var serializedPayload = RandomGenerator.GetMessageSerializedPayload();

            socket.Setup(s => s.Connected)
                .Returns(true);
            
            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
                .ReturnsAsync(serializedPayload.Data.Length);
            
            var client = new MessageBroker.Core.Clients.Client(socket.Object,
                NullLogger<MessageBroker.Core.Clients.Client>.Instance);

            var ticket = client.Enqueue(serializedPayload);

            var didReceiveNack = false;

            ticket.OnStatusChanged += (guid, b) =>
            {
                if (!b)
                {
                    didReceiveNack = true;
                }
            };

            client.SendNextMessageInQueue();
            
            client.OnPayloadNackReceived(serializedPayload.PayloadId);

            Thread.Yield();
            
            Assert.True(didReceiveNack);
        }
        
        [Fact]
        public void SendAsync_SocketReturnsZero_CloseIsCalled()
        {
            var socket = new Mock<ISocket>();

            var serializedPayload = RandomGenerator.GetMessageSerializedPayload();

            socket.Setup(s => s.Connected)
                .Returns(true);
            
            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
                .ReturnsAsync(0);
            
            var client = new MessageBroker.Core.Clients.Client(socket.Object,
                NullLogger<MessageBroker.Core.Clients.Client>.Instance);

            client.Enqueue(serializedPayload);

            client.SendNextMessageInQueue();
            
            Assert.True(client.IsClosed);
        }
        
        [Fact]
        public void Close_AnyCondition_NackStatusIsSetForAllPendingTickets()
        {
            var socket = new Mock<ISocket>();

            var serializedPayload = RandomGenerator.GetMessageSerializedPayload();

            socket.Setup(s => s.Connected)
                .Returns(true);
            
            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
                .ReturnsAsync(serializedPayload.Data.Length);
            
            var client = new MessageBroker.Core.Clients.Client(socket.Object,
                NullLogger<MessageBroker.Core.Clients.Client>.Instance);

            var ticket = client.Enqueue(serializedPayload);
            
            client.SendNextMessageInQueue();

            var didReceiveNack = false;

            ticket.OnStatusChanged += (_, b) =>
            {
                if (!b)
                {
                    didReceiveNack = true;
                }
            };

            client.Close();
            
            Assert.True(didReceiveNack);
        }
        
        [Fact]
        public void Close_AnyCondition_CallEnqueueWillCauseException()
        {
            var socket = new Mock<ISocket>();

            var serializedPayload = RandomGenerator.GetMessageSerializedPayload();

            socket.Setup(s => s.Connected)
                .Returns(true);
            
            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
                .ReturnsAsync(serializedPayload.Data.Length);
            
            var client = new MessageBroker.Core.Clients.Client(socket.Object,
                NullLogger<MessageBroker.Core.Clients.Client>.Instance);
            
            client.Close();
            
            Assert.Throws<ChannelClosedException>(() => client.Enqueue(serializedPayload));
        }
        
        [Fact]
        public void Close_AnyCondition_OnDisconnectedIsInvoked()
        {
            var socket = new Mock<ISocket>();

            var serializedPayload = RandomGenerator.GetMessageSerializedPayload();

            socket.Setup(s => s.Connected)
                .Returns(true);
            
            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>(), CancellationToken.None))
                .ReturnsAsync(serializedPayload.Data.Length);
            
            var client = new MessageBroker.Core.Clients.Client(socket.Object,
                NullLogger<MessageBroker.Core.Clients.Client>.Instance);

            var onDisconnectedWasCalled = false;

            client.OnDisconnected += (sender, args) =>
            {
                onDisconnectedWasCalled = true;
            };
            
            client.Close();

            Thread.Sleep(1000);
            
            Assert.True(onDisconnectedWasCalled);
        }
    }
}