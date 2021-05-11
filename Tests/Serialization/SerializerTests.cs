// using System;
// using System.Text;
// using MessageBroker.Models;
// using MessageBroker.Serialization;
// using Tests.Classes;
// using Xunit;
//
// namespace Tests.Serialization
// {
//     public class SerializerTests
//     {
//         private readonly Serializer _serializer;
//
//         public SerializerTests()
//         {
//             _serializer = new Serializer();
//         }
//
//         [Fact]
//         public void TestParseMessage()
//         {
//             var msg = new Message
//             {
//                 Id = Guid.NewGuid(),
//                 Route = RandomGenerator.GenerateString(10),
//                 Data = Encoding.UTF8.GetBytes(RandomGenerator.GenerateString(10))
//             };
//
//             var b = _serializer.Serialize(msg);
//
//             var convertedMsg = _serializer.ToMessage(b.DataWithoutSize);
//
//             Assert.Equal(msg.Id, convertedMsg.Id);
//             Assert.Equal(msg.Route, convertedMsg.Route);
//             Assert.Equal(Encoding.UTF8.GetString(msg.Data.Span), Encoding.UTF8.GetString(convertedMsg.Data.Span));
//         }
//
//         [Fact]
//         public void TestAck()
//         {
//             var ack = new Ack {Id = Guid.NewGuid()};
//
//             var b = _serializer.Serialize(ack);
//
//             var convertedAck = _serializer.ToAck(b.DataWithoutSize);
//
//             Assert.Equal(ack.Id, convertedAck.Id);
//         }
//
//         [Fact]
//         public void TestNack()
//         {
//             var nack = new Nack {Id = Guid.NewGuid()};
//
//             var b = _serializer.Serialize(nack);
//
//             var convertedNack = _serializer.ToAck(b.DataWithoutSize);
//
//             Assert.Equal(nack.Id, convertedNack.Id);
//         }
//
//         [Fact]
//         public void TestSubscribeQueue()
//         {
//             var subscribeQueue = new SubscribeQueue
//                 {Id = Guid.NewGuid(), QueueName = RandomGenerator.GenerateString(10)};
//
//             var b = _serializer.Serialize(subscribeQueue);
//
//             var converted = _serializer.ToSubscribeQueue(b.DataWithoutSize);
//
//             Assert.Equal(subscribeQueue.Id, converted.Id);
//             Assert.Equal(subscribeQueue.QueueName, converted.QueueName);
//         }
//
//         [Fact]
//         public void TestUnSubscribeQueue()
//         {
//             var unsubscribeQueue = new SubscribeQueue
//                 {Id = Guid.NewGuid(), QueueName = RandomGenerator.GenerateString(10)};
//
//             var b = _serializer.Serialize(unsubscribeQueue);
//
//             var converted = _serializer.ToSubscribeQueue(b.DataWithoutSize);
//
//             Assert.Equal(unsubscribeQueue.Id, converted.Id);
//             Assert.Equal(unsubscribeQueue.QueueName, converted.QueueName);
//         }
//
//
//         [Fact]
//         public void TestQueueDeclare()
//         {
//             var queue = new QueueDeclare
//             {
//                 Id = Guid.NewGuid(), Name = RandomGenerator.GenerateString(10),
//                 Route = RandomGenerator.GenerateString(10)
//             };
//
//             var b = _serializer.Serialize(queue);
//
//             var convertedQueueDeclare = _serializer.ToQueueDeclareModel(b.DataWithoutSize);
//
//             Assert.Equal(queue.Id, convertedQueueDeclare.Id);
//             Assert.Equal(queue.Name, convertedQueueDeclare.Name);
//             Assert.Equal(queue.Route, convertedQueueDeclare.Route);
//         }
//
//         [Fact]
//         public void TestQueueDelete()
//         {
//             var queue = new QueueDelete {Id = Guid.NewGuid(), Name = RandomGenerator.GenerateString(10)};
//
//             var b = _serializer.Serialize(queue);
//
//             var convertedQueueDelete = _serializer.ToQueueDeleteModel(b.DataWithoutSize);
//
//             Assert.Equal(queue.Id, convertedQueueDelete.Id);
//             Assert.Equal(queue.Name, convertedQueueDelete.Name);
//         }
//
//         [Fact]
//         public void TestError()
//         {
//             var error = new Error {Id = Guid.NewGuid(), Message = RandomGenerator.GenerateString(10)};
//
//             var b = _serializer.Serialize(error);
//
//             var convertedQueueDelete = _serializer.ToError(b.DataWithoutSize);
//
//             Assert.Equal(error.Id, convertedQueueDelete.Id);
//             Assert.Equal(error.Message, convertedQueueDelete.Message);
//         }
//     }
// }