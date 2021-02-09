using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MessageBroker.Client.EventStores;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.TaskManager
{
    public class DefaultTaskManager : ITaskManager
    {
        private readonly ConcurrentDictionary<Guid, SendTaskCompletionSource> _tasks;

        public DefaultTaskManager()
        {
            _tasks = new ConcurrentDictionary<Guid, SendTaskCompletionSource>();
        }

        public Task<SendAsyncResult> Setup(Guid id, bool completeOnAcknowledge)
        {
            var tcs = new TaskCompletionSource<SendAsyncResult>();

            var data = new SendTaskCompletionSource
            {
                CompleteOnAcknowledge = completeOnAcknowledge,
                TaskCompletionSource = tcs
            };

            _tasks[id] = data;

            return tcs.Task;
        }

        public void OnPayloadEvent(Guid payloadId, SendEventType ev, string error)
        {
            if (_tasks.TryGetValue(payloadId, out var data))
                switch (ev)
                {
                    case SendEventType.Ack:
                        data.OnAcknowledgeResult(true, null);
                        break;
                    case SendEventType.Nack:
                        data.OnAcknowledgeResult(false, error);
                        break;
                    case SendEventType.Sent:
                        data.OnSendResult(true, null);
                        break;
                    case SendEventType.Failed:
                        data.OnSendResult(false, error);
                        break;
                }
        }
    }
}