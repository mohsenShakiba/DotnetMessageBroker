using System;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Client.QueueManagement
{
    // public class QueueManagerTests
    // {
    //     [Fact]
    //     public void DeclareQueue_CreateSerializedPayload_ReturnSuccess()
    //     {
    //         // create mocks
    //         var serializer = new Mock<ISerializer>();
    //         var connectionManager = new Mock<IConnectionManager>();
    //         var queueManagerStore = new Mock<ISubscriberStore>();
    //         var sendPayloadTaskManager = new Mock<ISendPayloadTaskManager>();
    //         var clientSession = new Mock<IClientSession>();
    //         
    //         // declare variables
    //         var serializedPayload = RandomGenerator.SerializedPayload(PayloadType.QueueCreate);
    //         var successSendAsyncResult = new SendAsyncResult {IsSuccess = true};
    //         var queue = RandomGenerator.GenerateString(10);
    //         var route = RandomGenerator.GenerateString(10);
    //         
    //         // setup mocks
    //         serializer.Setup(s => s.Serialize(It.IsAny<QueueDeclare>())).Returns(serializedPayload);
    //         sendPayloadTaskManager.Setup(t => t.Setup(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(successSendAsyncResult);
    //         connectionManager.Setup(cm => cm.ClientSession).Returns(clientSession.Object);
    //         clientSession.Setup(cs => cs.SendAsync(It.IsAny<Memory<byte>>())).ReturnsAsync(true);
    //
    //         // setup UUT
    //         var queueManager = new Subscriber(serializer.Object, 
    //             connectionManager.Object,
    //             sendPayloadTaskManager.Object,
    //             queueManagerStore.Object);
    //         
    //         queueManager.Setup(queue, route);
    //
    //         var task = queueManager.DeclareQueue();
    //         var taskResult = task.Result;
    //
    //         // verify data passed to IClientSession is valid
    //         clientSession.Verify(cs => cs.SendAsync(serializedPayload.Data));
    //         
    //         // verify the returned task was successful
    //         Assert.True(taskResult.IsSuccess);
    //         
    //     }
    //
    //     [Fact]
    //     public void DeleteQueue_CreateSerializedPayload_ReturnSuccess()
    //     {
    //         // create mocks
    //         var serializer = new Mock<ISerializer>();
    //         var connectionManager = new Mock<IConnectionManager>();
    //         var queueManagerStore = new Mock<ISubscriberStore>();
    //         var sendPayloadTaskManager = new Mock<ISendPayloadTaskManager>();
    //         var clientSession = new Mock<IClientSession>();
    //         
    //         // declare variables
    //         var serializedPayload = RandomGenerator.SerializedPayload(PayloadType.QueueCreate);
    //         var successSendAsyncResult = new SendAsyncResult {IsSuccess = true};
    //         var queue = RandomGenerator.GenerateString(10);
    //         var route = RandomGenerator.GenerateString(10);
    //         
    //         // setup mocks
    //         serializer.Setup(s => s.Serialize(It.IsAny<QueueDelete>())).Returns(serializedPayload);
    //         sendPayloadTaskManager.Setup(t => t.Setup(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(successSendAsyncResult);
    //         connectionManager.Setup(cm => cm.ClientSession).Returns(clientSession.Object);
    //         clientSession.Setup(cs => cs.SendAsync(It.IsAny<Memory<byte>>())).ReturnsAsync(true);
    //
    //         // setup UUT
    //         var queueManager = new Subscriber(serializer.Object, 
    //             connectionManager.Object,
    //             sendPayloadTaskManager.Object,
    //             queueManagerStore.Object);
    //         
    //         queueManager.Setup(queue, route);
    //
    //         var task = queueManager.DeleteQueue();
    //         var taskResult = task.Result;
    //
    //         // verify data passed to IClientSession is valid
    //         clientSession.Verify(cs => cs.SendAsync(serializedPayload.Data));
    //         
    //         // verify the returned task was successful
    //         Assert.True(taskResult.IsSuccess);
    //     }
    //
    //
    //     [Fact]
    //     public void SubscribeQueue_CreateSerializedPayload_ReturnSuccess()
    //     {
    //         // create mocks
    //         var serializer = new Mock<ISerializer>();
    //         var connectionManager = new Mock<IConnectionManager>();
    //         var queueManagerStore = new Mock<ISubscriberStore>();
    //         var sendPayloadTaskManager = new Mock<ISendPayloadTaskManager>();
    //         var clientSession = new Mock<IClientSession>();
    //         
    //         // declare variables
    //         var serializedPayload = RandomGenerator.SerializedPayload(PayloadType.QueueCreate);
    //         var successSendAsyncResult = new SendAsyncResult {IsSuccess = true};
    //         var queue = RandomGenerator.GenerateString(10);
    //         var route = RandomGenerator.GenerateString(10);
    //         
    //         // setup mocks
    //         serializer.Setup(s => s.Serialize(It.IsAny<SubscribeQueue>())).Returns(serializedPayload);
    //         sendPayloadTaskManager.Setup(t => t.Setup(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(successSendAsyncResult);
    //         connectionManager.Setup(cm => cm.ClientSession).Returns(clientSession.Object);
    //         clientSession.Setup(cs => cs.SendAsync(It.IsAny<Memory<byte>>())).ReturnsAsync(true);
    //
    //         // setup UUT
    //         var queueManager = new Subscriber(serializer.Object, 
    //             connectionManager.Object,
    //             sendPayloadTaskManager.Object,
    //             queueManagerStore.Object);
    //         
    //         queueManager.Setup(queue, route);
    //
    //         var task = queueManager.SubscribeQueue();
    //         var taskResult = task.Result;
    //
    //         // verify data passed to IClientSession is valid
    //         clientSession.Verify(cs => cs.SendAsync(serializedPayload.Data));
    //         
    //         // verify the returned task was successful
    //         Assert.True(taskResult.IsSuccess);
    //     }
    //     
    //     [Fact]
    //     public void UnsubscribeQueue_CreateSerializedPayload_ReturnSuccess()
    //     {
    //         // create mocks
    //         var serializer = new Mock<ISerializer>();
    //         var connectionManager = new Mock<IConnectionManager>();
    //         var queueManagerStore = new Mock<ISubscriberStore>();
    //         var sendPayloadTaskManager = new Mock<ISendPayloadTaskManager>();
    //         var clientSession = new Mock<IClientSession>();
    //         
    //         // declare variables
    //         var serializedPayload = RandomGenerator.SerializedPayload(PayloadType.UnSubscribeQueue);
    //         var successSendAsyncResult = new SendAsyncResult {IsSuccess = true};
    //         var queue = RandomGenerator.GenerateString(10);
    //         var route = RandomGenerator.GenerateString(10);
    //         
    //         // setup mocks
    //         serializer.Setup(s => s.Serialize(It.IsAny<UnsubscribeQueue>())).Returns(serializedPayload);
    //         sendPayloadTaskManager.Setup(t => t.Setup(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(successSendAsyncResult);
    //         connectionManager.Setup(cm => cm.ClientSession).Returns(clientSession.Object);
    //         clientSession.Setup(cs => cs.SendAsync(It.IsAny<Memory<byte>>())).ReturnsAsync(true);
    //
    //         // setup UUT
    //         var queueManager = new Subscriber(serializer.Object, 
    //             connectionManager.Object,
    //             sendPayloadTaskManager.Object,
    //             queueManagerStore.Object);
    //         
    //         queueManager.Setup(queue, route);
    //
    //         var task = queueManager.UnSubscribeQueue();
    //         var taskResult = task.Result;
    //
    //         // verify data passed to IClientSession is valid
    //         clientSession.Verify(cs => cs.SendAsync(serializedPayload.Data));
    //         
    //         // verify the returned task was successful
    //         Assert.True(taskResult.IsSuccess);
    //     }
    // }
}