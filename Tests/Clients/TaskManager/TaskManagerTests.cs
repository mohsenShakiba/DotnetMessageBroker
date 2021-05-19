using System;
using System.Threading;
using MessageBroker.Client.TaskManager;
using Xunit;

namespace Tests.Clients.TaskManager
{
    public class TaskManagerTests
    {
        [Fact]
        public void GetTask_OkReceived_TaskCompletes()
        {
            var taskManager = new MessageBroker.Client.TaskManager.TaskManager();

            var payloadId = Guid.NewGuid();

            var task = taskManager.Setup(payloadId, true, CancellationToken.None);

            taskManager.OnPayloadOkResult(payloadId);

            var result = task.Result;

            Assert.True(result.IsSuccess);
            Assert.Null(result.InternalErrorCode);
        }

        [Fact]
        public void GetTask_ErrorReceived_TaskCompletesWithError()
        {
            var taskManager = new MessageBroker.Client.TaskManager.TaskManager();

            var payloadId = Guid.NewGuid();

            var task = taskManager.Setup(payloadId, true, CancellationToken.None);

            taskManager.OnPayloadErrorResult(payloadId, "error");

            var result = task.Result;

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.InternalErrorCode);
        }

        [Fact]
        public void GetTask_SendSuccessAndTaskWaitsOnSend_TaskCompletes()
        {
            var taskManager = new MessageBroker.Client.TaskManager.TaskManager();

            var payloadId = Guid.NewGuid();

            var task = taskManager.Setup(payloadId, false, CancellationToken.None);

            taskManager.OnPayloadSendSuccess(payloadId);

            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void GetTask_SendSuccessAndTaskWaitsOnOk_TaskDoesNotComplete()
        {
            var taskManager = new MessageBroker.Client.TaskManager.TaskManager();

            var payloadId = Guid.NewGuid();

            var task = taskManager.Setup(payloadId, true, CancellationToken.None);

            taskManager.OnPayloadSendSuccess(payloadId);

            Assert.False(task.IsCompleted);
        }

        [Fact]
        public void GetTask_SendFail_TaskCompletesWithError()
        {
            var taskManager = new MessageBroker.Client.TaskManager.TaskManager();

            var payloadId = Guid.NewGuid();

            var task = taskManager.Setup(payloadId, true, CancellationToken.None);

            taskManager.OnPayloadSendFailed(payloadId);

            var result = task.Result;

            Assert.False(result.IsSuccess);
            Assert.NotNull(result.InternalErrorCode);
        }
    }
}

