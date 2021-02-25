using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Core;
using MessageBroker.Core.Queues;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;
using MessageBroker.TCP.Client;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Core
{
    public class SendQueueTests
    {
        [Fact]
        public void TestSendQueueMakeSureSocketSendAsyncIsCalled()
        {
            var clientSession = new Mock<IClientSession>();
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload(PayloadType.Msg);
            var serializedNonMessagePayload = RandomGenerator.SerializedPayload(PayloadType.Ok);
            
            var sendQueue = new SendQueue(clientSession.Object);
            
            sendQueue.Configure(1000, true);
            
            sendQueue.Enqueue(serializedMessagePayload);
            sendQueue.Enqueue(serializedNonMessagePayload);
            
            Thread.Sleep(1000);
            
            clientSession.Verify(s => s.SendAsync(serializedMessagePayload.Data));
            clientSession.Verify(s => s.SendAsync(serializedNonMessagePayload.Data));
            
            clientSession.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestSendQueueMakeSureAckIsNotCalledWhenAutoAckIsDisabled()
        {
            var clientSession = new Mock<IClientSession>();
            
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
            sendQueue.Enqueue(serializedMessagePayload);

            var ackWasCalled = false;

            serializedMessagePayload.OnStatusChanged += (_, _) =>
            {
                ackWasCalled = true;
            };

            Thread.Sleep(100);
            
            Assert.False(ackWasCalled);

        }

        [Fact]
        public void TestSendQueueMakeSureAckIsCalledWhenAutoAckIsEnabled()
        {
            var clientSession = new Mock<IClientSession>();
            
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, true);
            sendQueue.Enqueue(serializedMessagePayload);

            var ackWasCalled = false;

            serializedMessagePayload.OnStatusChanged += (_, statusUpdate) =>
            {
                if (statusUpdate == SerializedPayloadStatusUpdate.Ack)
                    ackWasCalled = true;
            };

            Thread.Sleep(100);
            
            Assert.True(ackWasCalled);
        }

        [Fact]
        public void TestSendQueueMakeSureNackIsCalledWhenPayloadIsNotSent()
        {
            var clientSession = new Mock<IClientSession>();

            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(false);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, true);
            sendQueue.Enqueue(serializedMessagePayload);
            
            var nackWasCalled = false;

            serializedMessagePayload.OnStatusChanged += (_, statusUpdate) =>
            {
                if (statusUpdate == SerializedPayloadStatusUpdate.Nack)
                    nackWasCalled = true;
            };
            
            Thread.Sleep(100);
            
            Assert.True(nackWasCalled);
        }
        
        [Fact]
        public void TestSendQueueMakeSureAvailableIsDecreasedWhenEnqueueIsCalledAndThenIncreasedWhenReleaseIsCalled()
        {
            var clientSession = new Mock<IClientSession>();
            
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
 
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
            sendQueue.Enqueue(serializedMessagePayload);
            
            Thread.Sleep(100);
            
            Assert.Equal(0, sendQueue.Available);
            
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
            
            Assert.Equal(1, sendQueue.Available);
        }

        [Fact]
        public void TestSendQueueMakeSureAvailableIsProperlySetWhenConfigureIsCalledAgain()
        {
            var clientSession = new Mock<IClientSession>();

            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(5, false);
            
            sendQueue.Enqueue(serializedMessagePayload);
            
            Thread.Sleep(100);

            sendQueue.Configure(5, true);
            
            // the value must be 4 and not 5
            Assert.Equal(4, sendQueue.Available);

        }
        
        [Fact]
        public void TestMultipleCallsToAckAndNackWithSameMessageIdHasNoEffectOnAvailable()
        {
            var clientSession = new Mock<IClientSession>();
            
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
 
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
            sendQueue.Enqueue(serializedMessagePayload);
            
            Thread.Sleep(100);
            
            Assert.Equal(0, sendQueue.Available);
            
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
            
            Assert.Equal(1, sendQueue.Available);
        }

        [Fact]
        public void TestSendQueueMakeSureWhenQueueIsFullAMessageIsSentOnlyWhenReleaseIsCalled()
        {
            var clientSession = new Mock<IClientSession>();

            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload1 = RandomGenerator.SerializedPayload();
            var serializedMessagePayload2 = RandomGenerator.SerializedPayload();
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
            
            sendQueue.Enqueue(serializedMessagePayload1);
            sendQueue.Enqueue(serializedMessagePayload2);

            Thread.Sleep(100);
            
            clientSession.Verify(c => c.SendAsync(It.IsAny<Memory<byte>>()));
            clientSession.VerifyNoOtherCalls();
            
            sendQueue.OnMessageAckReceived(serializedMessagePayload1.Id);
            
            Thread.Sleep(100);
            
            clientSession.Verify(c => c.SendAsync(It.IsAny<Memory<byte>>()));
            clientSession.VerifyNoOtherCalls();
        }
        

        [Fact]
        public void TestSendQueueMakeSureDisposeIsCalledOnQueueSendPayloadWhenAutoAckIsActive()
        {
            var clientSession = new Mock<IClientSession>();

            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, true);
            
            sendQueue.Enqueue(serializedMessagePayload);
            
            Thread.Sleep(1000);
            
            Assert.True(serializedMessagePayload.IsReturnedToPool);

        }

        [Fact]
        public void TestSendQueueMakeSureDisposeIsCalledOnQueueSendPayloadWhenAutoAckIsDeactive()
        {
            var clientSession = new Mock<IClientSession>();

            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
            
            sendQueue.Enqueue(serializedMessagePayload);
            
            Thread.Sleep(1000);
            
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
            
            Assert.True(serializedMessagePayload.IsReturnedToPool);
        }

        [Fact]
        public void TestSendQueueMakeSureNackIsCalledWhenSendQueueIsStopped()
        {
            var clientSession = new Mock<IClientSession>();

            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
            
            var serializedMessagePayload1 = RandomGenerator.SerializedPayload();
            var serializedMessagePayload2 = RandomGenerator.SerializedPayload();
            
            serializedMessagePayload2.Id = Guid.Empty;
            
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
            
            sendQueue.Enqueue(serializedMessagePayload1);
            sendQueue.Enqueue(serializedMessagePayload2);
            
            Thread.Sleep(100);

            var serializedMessagePayload1NackReceived = false;
            var serializedMessagePayload2NackReceived = false;

            serializedMessagePayload1.OnStatusChanged += (_, statusUpdate) =>
            {
                if (statusUpdate == SerializedPayloadStatusUpdate.Nack)
                    serializedMessagePayload1NackReceived = true;
            };

            serializedMessagePayload2.OnStatusChanged += (_, statusUpdate) =>
            {
                if (statusUpdate == SerializedPayloadStatusUpdate.Nack)
                    serializedMessagePayload2NackReceived = true;
            };
            
            sendQueue.Stop();
            
            Thread.Sleep(100);
            
            Assert.True(serializedMessagePayload1NackReceived);
            Assert.True(serializedMessagePayload2NackReceived);
        }
    }
}