// using System;
// using MessageBroker.Client.TaskManager;
// using Xunit;
//
// namespace Tests.Client.TaskManager
// {
//     public class TaskManagerTests
//     {
//         [Fact]
//         public void GetTask_OkReceived_TaskCompletes()
//         {
//             var taskManager = new SendPayloadTaskManager();
//
//             var payloadId = Guid.NewGuid();
//
//             var task = taskManager.Setup(payloadId, true);
//
//             taskManager.OnPayloadOkResult(payloadId);
//
//             var result = task.Result;
//
//             Assert.True(result.IsSuccess);
//             Assert.Null(result.InternalErrorCode);
//         }
//
//         [Fact]
//         public void GetTask_ErrorReceived_TaskCompletesWithError()
//         {
//             var taskManager = new SendPayloadTaskManager();
//
//             var payloadId = Guid.NewGuid();
//
//             var task = taskManager.Setup(payloadId, true);
//
//             taskManager.OnPayloadErrorResult(payloadId, "error");
//
//             var result = task.Result;
//
//             Assert.False(result.IsSuccess);
//             Assert.NotNull(result.InternalErrorCode);
//         }
//
//         [Fact]
//         public void GetTask_SendSuccessAndTaskWaitsOnSend_TaskCompletes()
//         {
//             var taskManager = new SendPayloadTaskManager();
//
//             var payloadId = Guid.NewGuid();
//
//             var task = taskManager.Setup(payloadId, false);
//
//             taskManager.OnPayloadSendSuccess(payloadId);
//
//             Assert.True(task.IsCompleted);
//         }
//
//         [Fact]
//         public void GetTask_SendSuccessAndTaskWaitsOnOk_TaskDoesNotComplete()
//         {
//             var taskManager = new SendPayloadTaskManager();
//
//             var payloadId = Guid.NewGuid();
//
//             var task = taskManager.Setup(payloadId, true);
//
//             taskManager.OnPayloadSendSuccess(payloadId);
//
//             Assert.False(task.IsCompleted);
//         }
//
//         [Fact]
//         public void GetTask_SendFail_TaskCompletesWithError()
//         {
//             var taskManager = new SendPayloadTaskManager();
//
//             var payloadId = Guid.NewGuid();
//
//             var task = taskManager.Setup(payloadId, true);
//
//             taskManager.OnPayloadSendFailed(payloadId);
//
//             var result = task.Result;
//
//             Assert.False(result.IsSuccess);
//             Assert.NotNull(result.InternalErrorCode);
//         }
//     }
// }