using System;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Dispatching;
using Moq;
using Xunit;

namespace Tests.Core.Dispatching
{
    public class DefaultSessionPolicyTests
    {
        [Fact]
        public void NextAvailable_NoClient_ResultIsNull()
        {
            var dispatcher = new DefaultDispatcher();
            var result = dispatcher.NextAvailable();
            Assert.Null(result);
        }
        
        [Fact]
        public void NextAvailable_WithAvailableClient_ResultIsNull()
        {
            var mockSendQueue = new Mock<IClient>();
            
            var dispatcher = new DefaultDispatcher();
            
            dispatcher.Add(mockSendQueue.Object);
            
            var result = dispatcher.NextAvailable();
            
            Assert.NotNull(result);
        }

    }
}