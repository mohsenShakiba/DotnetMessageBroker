// using System;
// using MessageBroker.Client.QueueConsumerCoordination;
// using MessageBroker.Client.ReceiveDataProcessing;
// using MessageBroker.Client.TaskManager;
// using MessageBroker.Models;
// using MessageBroker.Serialization;
// using Moq;
// using Tests.Classes;
// using Xunit;
//
// namespace Tests.Client.ReceiveDataProcessing
// {
//     public class ReceiveDataProcessorTests
//     {
//         [Fact]
//         public void DataReceived_DataIsMessage_DispatchToQueueManagerStore()
//         {
//             var serializer = new Serializer();
//             var queueManagerStore = new Mock<ISubscriberStore>();
//             var sendPayloadTaskManager = new Mock<ISendPayloadTaskManager>();
//
//             var receiveDataProcessor = new ReceiveDataProcessor(serializer,
//                 queueManagerStore.Object,
//                 sendPayloadTaskManager.Object);
//
//             var message = new QueueMessage()
//             {
//                 Id = Guid.NewGuid(),
//                 Route = RandomGenerator.GenerateString(10),
//                 QueueName = RandomGenerator.GenerateString(10),
//                 Data = RandomGenerator.GenerateBytes(10)
//             };
//             
//             var realSerializer = new Serializer();
//             var serializedPayload = realSerializer.Serialize(message);
//             
//             receiveDataProcessor.DataReceived(Guid.Empty, serializedPayload.DataWithoutSize);
//             
//             queueManagerStore.Verify(qms => qms.OnMessage(It.IsAny<QueueMessage>()));
//         }
//
//         [Fact]
//         public void DataReceived_DataIsOk_TaskManagerIsCalled()
//         {
//             var serializer = new Serializer();
//             var queueManagerStore = new Mock<ISubscriberStore>();
//             var sendPayloadTaskManager = new Mock<ISendPayloadTaskManager>();
//
//             var receiveDataProcessor = new ReceiveDataProcessor(serializer,
//                 queueManagerStore.Object,
//                 sendPayloadTaskManager.Object);
//             
//             var ok = new Ok()
//             {
//                 Id = Guid.NewGuid(),
//             };
//             
//             var realSerializer = new Serializer();
//             var serializedPayload = realSerializer.Serialize(ok);
//             
//             receiveDataProcessor.DataReceived(Guid.Empty, serializedPayload.DataWithoutSize);
//     
//             sendPayloadTaskManager.Verify(stm => stm.OnPayloadOkResult(It.IsAny<Guid>()));
//         }
//         
//         [Fact]
//         public void DataReceived_DataIsError_TaskManagerIsCalled()
//         {
//             var serializer = new Serializer();
//             var queueManagerStore = new Mock<ISubscriberStore>();
//             var sendPayloadTaskManager = new Mock<ISendPayloadTaskManager>();
//
//             var receiveDataProcessor = new ReceiveDataProcessor(serializer,
//                 queueManagerStore.Object,
//                 sendPayloadTaskManager.Object);
//             
//             var error = new Error
//             {
//                 Id = Guid.NewGuid(),
//                 Message = RandomGenerator.GenerateString(10)
//             };
//             
//             var realSerializer = new Serializer();
//             var serializedPayload = realSerializer.Serialize(error);
//             
//             receiveDataProcessor.DataReceived(Guid.Empty, serializedPayload.DataWithoutSize);
//     
//             sendPayloadTaskManager.Verify(stm => stm.OnPayloadErrorResult(It.IsAny<Guid>(), It.IsAny<string>()));
//         }
//     }
// }