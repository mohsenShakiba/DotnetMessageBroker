using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
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
        
            sendQueue.ReadNextPayloadAsync();
            sendQueue.ReadNextPayloadAsync();
        
            clientSession.Verify(s => s.SendAsync(serializedMessagePayload.Data));
            clientSession.Verify(s => s.SendAsync(serializedNonMessagePayload.Data));
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
        
            serializedMessagePayload.OnStatusChanged += (_, _) => { ackWasCalled = true; };
        
            Thread.Sleep(100);
        
            Assert.False(ackWasCalled);
        }

        [Fact]
        public async Task TestSendQueueMakeSureAckIsCalledWhenAutoAckIsEnabled()
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
            
            await sendQueue.ReadNextPayloadAsync();

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
        
            sendQueue.ReadNextPayloadAsync();
        
            Assert.True(nackWasCalled);
        }
        
        [Fact]
        public async Task TestSendQueueMakeSureAvailableIsDecreasedWhenEnqueueIsCalledAndThenIncreasedWhenReleaseIsCalled()
        {
            var clientSession = new Mock<IClientSession>();
        
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
        
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
        
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
            sendQueue.Enqueue(serializedMessagePayload);
        
            await sendQueue.ReadNextPayloadAsync();
            
            Assert.Equal(0, sendQueue.AvailableCount);
        
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
        
            Assert.Equal(1, sendQueue.AvailableCount);
        }
        
        [Fact]
        public async Task TestSendQueueMakeSureAvailableIsProperlySetWhenConfigureIsCalledAgain()
        {
            var clientSession = new Mock<IClientSession>();
        
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
        
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
        
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(5, false);
        
            sendQueue.Enqueue(serializedMessagePayload);
        
            await sendQueue.ReadNextPayloadAsync();
        
            sendQueue.Configure(5, true);
        
            // the value must be 4 and not 5
            Assert.Equal(4, sendQueue.AvailableCount);
        }
        
        [Fact]
        public async Task TestMultipleCallsToAckAndNackWithSameMessageIdHasNoEffectOnAvailable()
        {
            var clientSession = new Mock<IClientSession>();
        
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
        
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
        
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
        
            sendQueue.Enqueue(serializedMessagePayload);
        
            await sendQueue.ReadNextPayloadAsync();
        
            Assert.Equal(0, sendQueue.AvailableCount);
        
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
        
            Assert.Equal(1, sendQueue.AvailableCount);
        }
        
        [Fact]
        public async Task TestSendQueueMakeSureDisposeIsCalledOnQueueSendPayloadWhenAutoAckIsActive()
        {
            var clientSession = new Mock<IClientSession>();
        
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
        
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
        
            var sendQueue = new SendQueue(clientSession.Object);
            
            sendQueue.Configure(1, true);
        
            sendQueue.Enqueue(serializedMessagePayload);
        
            await sendQueue.ReadNextPayloadAsync();
        
            Assert.True(serializedMessagePayload.IsReturnedToPool);
        }
        
        [Fact]
        public async Task TestSendQueueMakeSureDisposeIsCalledOnQueueSendPayloadWhenAutoAckIsDeactive()
        {
            var clientSession = new Mock<IClientSession>();
        
            clientSession
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .ReturnsAsync(true);
        
            var serializedMessagePayload = RandomGenerator.SerializedPayload();
        
            var sendQueue = new SendQueue(clientSession.Object);
            sendQueue.Configure(1, false);
        
            sendQueue.Enqueue(serializedMessagePayload);
        
            await sendQueue.ReadNextPayloadAsync();
        
            sendQueue.OnMessageAckReceived(serializedMessagePayload.Id);
        
            Assert.True(serializedMessagePayload.IsReturnedToPool);
        }
        
        [Fact]
        public async Task TestSendQueueMakeSureNackIsCalledWhenSendQueueIsStopped()
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

            Assert.True(serializedMessagePayload1NackReceived);
            Assert.True(serializedMessagePayload2NackReceived);
        }
        
        [Theory]
        [InlineData(1000, 100)]
        public void
            Enqueue_WhenMoreMessagesAreSentThanTheSendQueueCanPrefetchAndIClientSessionIsDisconnected_AllMessagesAreAckedOrNacked(
                int numberOfMessage, int prefetchCount)
        {
            Logger.AddFileLogger(@"C:\Users\m.shakiba.PSZ021-PC\Desktop\testo\logs.txt");
            
            var clientSessionMock = new Mock<IClientSession>();
        
            clientSessionMock
                .Setup(c => c.SendAsync(It.IsAny<Memory<byte>>()))
                .Callback(async () => { await Task.Delay(10); })
                .ReturnsAsync(true);
        
            var sendQueue = new SendQueue(clientSessionMock.Object);
            var messageList = new List<SerializedPayload>();
            var ackedOrNackedMessageList = new List<SerializedPayload>();
            
            sendQueue.Configure(prefetchCount, false);
        
            for (var i = 0; i < numberOfMessage; i++)
            {
                var msg = RandomGenerator.SerializedPayload();
                
                messageList.Add(msg);
                
                msg.OnStatusChanged += (guid, update) =>
                {
                    var msg = messageList.First(m => m.Id == guid);
                    ackedOrNackedMessageList.Add(msg);
                };
            }
        
            foreach (var msg in messageList)
            {
                sendQueue.Enqueue(msg);
            }
            
            Thread.Sleep(100);
            
            sendQueue.Stop();
        
            var allMessagesAreAckedOrNacked = messageList.Count == ackedOrNackedMessageList.Count();
        
            var diff = messageList.Where(m => ackedOrNackedMessageList.All(m2 => m != m2)).FirstOrDefault();
        
            if (diff is not null)
            {
                Logger.LogInformation($"message with id {diff.Id} was not found");
            }
            
            Assert.True(allMessagesAreAckedOrNacked);
        }
    }
}