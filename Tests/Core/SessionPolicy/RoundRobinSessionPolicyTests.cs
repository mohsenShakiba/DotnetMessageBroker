using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Core;
using MessageBroker.Core.SessionPolicy;
using Moq;
using Xunit;

namespace Tests.Core.SessionPolicy
{
    public class RoundRobinSessionPolicyTests
    {
        [Fact]
        public void GetNextAvailableSendQueueAsync_WhenNoSendQueueExists_TaskIsInPending()
        {
            var sessionPolicy = new DefaultSessionPolicy();
            var result = sessionPolicy.GetNextAvailableSendQueueAsync(CancellationToken.None);
            Assert.False(result.IsCompleted);
        }


        [Fact]
        public void GetNextAvailableSendQueueAsync_WhenSendQueueExistsButNotAvailable_TaskIsNotCompleted()
        {
            var mockSendQueue = new Mock<ISendQueue>();
            var sessionPolicy = new DefaultSessionPolicy();

            mockSendQueue.Setup(i => i.IsAvailable).Returns(false);
            
            sessionPolicy.AddSendQueue(mockSendQueue.Object);
            
            var result = sessionPolicy.GetNextAvailableSendQueueAsync(CancellationToken.None);
            
            Assert.False(result.IsCompleted);
        }
        
        [Fact]
        public void GetNextAvailableSendQueueAsync_WhenAvailable_TaskIsInCompleted()
        {
            var mockSendQueue = new Mock<ISendQueue>();
            var sessionPolicy = new DefaultSessionPolicy();

            mockSendQueue.Setup(i => i.IsAvailable).Returns(true);
            
            sessionPolicy.AddSendQueue(mockSendQueue.Object);
            
            var result = sessionPolicy.GetNextAvailableSendQueueAsync(CancellationToken.None);
            
            Assert.True(result.IsCompleted);
            Assert.Equal(mockSendQueue.Object, result.Result);
        }

        
        [Fact]
        public void GetNextAvailableSendQueueAsync_WhenDuplicateSendQueueIsAdded_ErrorIsThrown()
        {
            var sessionPolicy = new DefaultSessionPolicy();

            var duplicateId = Guid.NewGuid();

            var mockSendQueue = new Mock<ISendQueue>();

            mockSendQueue.SetupGet(i => i.Id).Returns(duplicateId);
            
            sessionPolicy.AddSendQueue(mockSendQueue.Object);

            Assert.Throws<Exception>(() =>
            {
                sessionPolicy.AddSendQueue(mockSendQueue.Object);
            });
        }

    }
}