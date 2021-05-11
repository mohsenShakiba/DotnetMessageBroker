// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using MessageBroker.Core;
// using MessageBroker.Core.Queues;
// using MessageBroker.Core.SessionPolicy;
// using Moq;
// using Xunit;
//
// namespace Tests.Core.SessionPolicy
// {
//     public class DefaultSessionPolicyTests
//     {
//         [Fact]
//         public void Next_WhenNoSendQueueExists_ResultIsNull()
//         {
//             var sessionPolicy = new DefaultDispatchPolicy();
//             var result = sessionPolicy.Next();
//             Assert.Null(result);
//         }
//         
//         [Fact]
//         public void Next_WhenSendQueueExists_ResultIsNull()
//         {
//             var mockSendQueue = new Mock<IQueue>();
//             
//             var sessionPolicy = new DefaultDispatchPolicy();
//             
//             sessionPolicy.AddSendQueue(mockSendQueue.Object);
//             
//             var result = sessionPolicy.Next();
//             
//             Assert.NotNull(result);
//         }
//
//         [Fact]
//         public void GetNextAvailableSendQueueAsync_WhenDuplicateSendQueueIsAdded_ErrorIsThrown()
//         {
//             var sessionPolicy = new DefaultDispatchPolicy();
//
//             var duplicateId = Guid.NewGuid();
//
//             var mockSendQueue = new Mock<IQueue>();
//
//             mockSendQueue.SetupGet(i => i.Id).Returns(duplicateId);
//             
//             sessionPolicy.AddSendQueue(mockSendQueue.Object);
//
//             Assert.Throws<Exception>(() =>
//             {
//                 sessionPolicy.AddSendQueue(mockSendQueue.Object);
//             });
//         }
//
//     }
// }