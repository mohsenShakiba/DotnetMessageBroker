using System;
using System.Threading;
using MessageBroker.Core;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Messages.InMemoryStore;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Core.Queues;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.SessionPolicy;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Classes;
using Xunit;

namespace Tests.Core.PayloadProcessing
{
    public class PayloadProcessorTests
    {
        // [Fact]
        // public void PayloadIsMessage_ParseTheMessage_SendToQueueAndReturnOkToPublisher()
        // {
        //     var message = new Message
        //     {
        //         Id = Guid.NewGuid(),
        //         Route = RandomGenerator.GenerateString(10),
        //         Data = RandomGenerator.GenerateBytes(100),
        //     };
        //
        //     var senderSessionId = Guid.NewGuid();
        //     
        //     var queue = new Mock<IQueue>();
        //     queue.Setup(q => q.MessageRouteMatch(It.IsAny<string>())).Returns(true);
        //     
        //     var queueStore = new Mock<IQueueStore>();
        //     queueStore.Setup(qs => qs.GetAll()).Returns(new[] {queue.Object});
        //     
        //     var sendQueue = new Mock<ISendQueue>();
        //     var sendQueueObject = sendQueue.Object;
        //     
        //     var sendQueueStore = new Mock<ISendQueueStore>();
        //     sendQueueStore.Setup(sqs => sqs.TryGet(It.IsAny<Guid>(), out sendQueueObject)).Returns(true);
        //     
        //     var serializer = new Mock<ISerializer>();
        //     serializer.Setup(s => s.ParsePayloadType(It.IsAny<Memory<byte>>())).Returns(PayloadType.Msg);
        //     serializer.Setup(s => s.ToMessage(It.IsAny<Memory<byte>>())).Returns(message);
        //     
        //     var payloadProcessor = new PayloadProcessor(serializer.Object, sendQueueStore.Object, queueStore.Object);
        //     
        //     var realSerializer = new Serializer();
        //     var serializedPayload = realSerializer.Serialize(message);
        //     
        //     payloadProcessor.OnDataReceived(senderSessionId, serializedPayload.DataWithoutSize);
        //     
        //     queue.Verify(q => q.OnMessage(It.IsAny<Message>()));
        //     sendQueue.Verify(q => q.Enqueue(It.IsAny<SerializedPayload>()));
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsAckAndNackItIsSentToSendQueue()
        // {
        //     var ack = new Ack
        //     {
        //         Id = Guid.NewGuid(),
        //     };
        //     
        //     var nack = new Nack
        //     {
        //         Id = Guid.NewGuid(),
        //     };
        //
        //     var senderSessionId = Guid.NewGuid();
        //     
        //     var queueStore = new Mock<IQueueStore>();
        //     
        //     var sendQueue = new Mock<ISendQueue>();
        //     var sendQueueObject = sendQueue.Object;
        //
        //     var sendQueueStore = new Mock<ISendQueueStore>();
        //     sendQueueStore.Setup(sqs => sqs.TryGet(It.IsAny<Guid>(), out sendQueueObject)).Returns(true);
        //
        //     var serializer = new Mock<ISerializer>();
        //     serializer.Setup(s => s.ParsePayloadType(It.IsAny<Memory<byte>>())).Returns(PayloadType.Msg);
        //     serializer.Setup(s => s.ToAck(It.IsAny<Memory<byte>>())).Returns(ack);
        //
        //     var payloadProcessor = new PayloadProcessor(serializer.Object, sendQueueStore.Object, queueStore.Object);
        //
        //     var realSerializer = new Serializer();
        //     var ackSerializedPayload = realSerializer.Serialize(ack);
        //     var nackSerializedPayload = realSerializer.Serialize(nack);
        //     
        //     payloadProcessor.OnDataReceived(senderSessionId, ackSerializedPayload.DataWithoutSize);
        //     payloadProcessor.OnDataReceived(senderSessionId, nackSerializedPayload.DataWithoutSize);
        //     
        //     sendQueue.Verify(q => q.Enqueue(It.IsAny<SerializedPayload>()));
        //     sendQueue.Verify(q => q.Enqueue(It.IsAny<SerializedPayload>()));
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsDeclareQueueThenOkIsSentToClientSession()
        // {
        //     var declareQueue = new QueueDeclare
        //     {
        //         Id = Guid.NewGuid(),
        //         Name = RandomGenerator.GenerateString(10),
        //         Route = RandomGenerator.GenerateString(10),
        //     };
        //
        //     var senderSessionId = Guid.NewGuid();
        //     
        //     var queue = new Mock<IQueue>();
        //     queue.Setup(q => q.MessageRouteMatch(It.IsAny<string>())).Returns(true);
        //     
        //     var queueStore = new Mock<IQueueStore>();
        //     queueStore.Setup(qs => qs.GetAll()).Returns(new[] {queue.Object});
        //
        //     var sendQueue = new Mock<ISendQueue>();
        //     var sendQueueObject = sendQueue.Object;
        //     
        //     var sendQueueStore = new Mock<ISendQueueStore>();
        //     sendQueueStore.Setup(sqs => sqs.TryGet(It.IsAny<Guid>(), out sendQueueObject)).Returns(true);
        //     
        //     var serializer = new Mock<ISerializer>();
        //     serializer.Setup(s => s.ParsePayloadType(It.IsAny<Memory<byte>>())).Returns(PayloadType.Msg);
        //     serializer.Setup(s => s.ToQueueDeclareModel(It.IsAny<Memory<byte>>())).Returns(declareQueue);
        //
        //     var payloadProcessor = new PayloadProcessor(serializer.Object, sendQueueStore.Object, queueStore.Object);
        //
        //     var realSerializer = new Serializer();
        //     var serializedPayload = realSerializer.Serialize(declareQueue);
        //     
        //     payloadProcessor.OnDataReceived(senderSessionId, serializedPayload.DataWithoutSize);
        //     
        //     queue.Verify(q => q.OnMessage(It.IsAny<Message>()));
        //     sendQueue.Verify(q => q.Enqueue(It.IsAny<SerializedPayload>()));
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsDeclareQueueAndTheQueueRouteDoesNotMatchThenErrorIsSentToClientSession()
        // {
        //     
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsDeleteQueueThenDeleteMethodOfQueueStoreIsCalledAndOkIsSentToClientSession()
        // {
        //     
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsSubscribeQueueAndQueueIsFoundThenOkIsSentToClientSession()
        // {
        //     
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsSubscribeQueueAndQueueIsNotFoundThenErrorIsSentToClientSession()
        // {
        //     
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsUnsubscribeQueueAndQueueIsFoundThenOkIsSentToClientSession()
        // {
        //     
        // }
        //
        // [Fact]
        // public void MakeSureWhenDataIsUnsubscribeQueueAndQueueIsNotFoundThenErrorIsSentToClientSession()
        // {
        //     
        // }
    }
}