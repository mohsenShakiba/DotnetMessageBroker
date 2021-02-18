using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Binary;
using MessageBroker.Models;
using MessageBroker.Serialization;
using MessageBroker.Socket.Client;
using MessageBroker.Socket.SocketWrapper;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.TCP.Client
{
    public class ClientTests
    {
        [Fact]
        public void TestMakeSureReceivedDataIsValid()
        {
            var resetEvent = new ManualResetEvent(false);
            
            var socket = new TestTcpSocket();
            var serializer = new Serializer();
            var socketEventProcessor = new TestSocketEventProcessor();
            var binaryDataProcessor = new BinaryDataProcessor();
            var clientSession = new ClientSession(binaryDataProcessor);
            
            clientSession.ForwardDataTo(socketEventProcessor);
            clientSession.ForwardEventsTo(socketEventProcessor);
            clientSession.Use(socket);

            var messageId = Guid.NewGuid();
            var messageRoute = RandomGenerator.GenerateString(10);
            var messageData = Encoding.UTF8.GetBytes(RandomGenerator.GenerateString(100));

                var testMessage = new Message
            {
                Id = messageId,
                Route = messageRoute,
                Data = messageData
            };
            
            socketEventProcessor.OnDataReceived += (id, memory) =>
            {
                var deserializedTestMessage = serializer.ToMessage(memory);

                if (deserializedTestMessage.Id == messageId &&
                    deserializedTestMessage.Route == messageRoute &&
                    Encoding.UTF8.GetString(messageData) == Encoding.UTF8.GetString(deserializedTestMessage.Data.Span))
                {
                    resetEvent.Set();
                }
                else
                {
                    throw new Exception("The data deserialization failed");
                }
            };


            var serializedTestMessage = serializer.Serialize(testMessage);

            var currentOffset = 0;
            var segmentSize = 10;
            
            while (currentOffset < serializedTestMessage.Data.Length)
            {
                if (currentOffset + segmentSize < serializedTestMessage.Data.Length)
                    socket.SendAsync(serializedTestMessage.Data.Slice(currentOffset, segmentSize));
                else
                    socket.SendAsync(serializedTestMessage.Data.Slice(currentOffset));
                currentOffset += segmentSize;
            }
            
            resetEvent.WaitOne();
        }

        [Fact]
        public void TestSocketIsClosedWhenSendDataReturnsInvalidSize()
        {
            var resetEvent = new ManualResetEvent(false);
            
            var socket = new Mock<ITcpSocket>();

            socket.Setup(s => s.SendAsync(It.IsAny<Memory<byte>>())).Returns(ValueTask.FromResult(0));
            
            var binaryDataProcessor = new BinaryDataProcessor();
            var clientSession = new ClientSession(binaryDataProcessor);
            var socketEventProcessor = new TestSocketEventProcessor();
   
            clientSession.ForwardDataTo(socketEventProcessor);
            clientSession.ForwardEventsTo(socketEventProcessor);
            clientSession.Use(socket.Object);


            socketEventProcessor.OnClientDisconnected += guid =>
            {
                resetEvent.Set();
            };
            
            clientSession.SendAsync(Memory<byte>.Empty);
            
            resetEvent.WaitOne();
        }

        [Fact]
        public void TestSocketIsClosedWhenReceiveDataReturnsInvalidSize()
        {
            var resetEvent = new ManualResetEvent(false);
            
            var socket = new Mock<ITcpSocket>();

            socket.Setup(s => s.ReceiveAsync(It.IsAny<Memory<byte>>())).Returns(ValueTask.FromResult(0));
            
            var binaryDataProcessor = new BinaryDataProcessor();
            var clientSession = new ClientSession(binaryDataProcessor);
            var socketEventProcessor = new TestSocketEventProcessor();
            
            socketEventProcessor.OnClientDisconnected += guid =>
            {
                resetEvent.Set();
            };
   
            clientSession.ForwardDataTo(socketEventProcessor);
            clientSession.ForwardEventsTo(socketEventProcessor);
            clientSession.Use(socket.Object);
            
            resetEvent.WaitOne();
        }
    }
}