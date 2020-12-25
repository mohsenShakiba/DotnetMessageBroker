using MessageBroker.Core;
using MessageBroker.Core.BufferPool;
using MessageBroker.Core.MessageRefStore;
using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class SendQueueTests
    {

        [Fact]
        public void TestEnqueuWhenFull()
        {
            var messageRefStore = new DefaultMessageRefStore();
            var bufferPool = new DefaultBufferPool();
            var serializer = new DefaultSerializer(bufferPool);
            var session = new Mock<IClientSession>();

            var messageOne = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST", 
                Data = Encoding.UTF8.GetBytes("TEST")
            };

            var messageTwo = new Message
            {
                Id = Guid.NewGuid(),
                Route = "TEST",
                Data = Encoding.UTF8.GetBytes("TEST")
            };

            var sendQueue = new SendQueue(session.Object, serializer, messageRefStore, 1, 0);

            // enqueu first message
            sendQueue.Enqueue(messageOne);

            // make sure send method was called
            session.Verify(session => session.Send(It.IsAny<Memory<byte>>()));

            // enqueue second message
            sendQueue.Enqueue(messageTwo);

            // make sure the session send was not called
            session.VerifyNoOtherCalls();

            // make sure the session has 
            Assert.Equal(1, sendQueue.CurrentCuncurrency);

            // when release is called, send method of session should be called 
            sendQueue.ReleaseOne(messageOne.Id);

            // verify send was called
            session.Verify(session => session.Send(It.IsAny<Memory<byte>>()));
        }

        [Fact]
        public void TestReleaseWhenMessageDoesNotExists()
        {
            var messageRefStore = new DefaultMessageRefStore();
            var bufferPool = new DefaultBufferPool();
            var serializer = new DefaultSerializer(bufferPool);
            var session = new Mock<IClientSession>();
            var sendQueue = new SendQueue(session.Object, serializer, messageRefStore, 1, 1);
            var randomId = Guid.NewGuid();

            sendQueue.ReleaseOne(randomId);

            Assert.Equal(1, sendQueue.CurrentCuncurrency);
        }


    }
}
