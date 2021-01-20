using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MessageBroker.Client.EventStores;

namespace MessageBroker.Client.TaskManager
{
    public class DefaultTaskManager : ITaskManager
    {
        private readonly IEventStore _eventStore;
        private readonly ConcurrentDictionary<Guid, PayloadTaskCompletionData> _tasks;

        public DefaultTaskManager(IEventStore eventStore)
        {
            _eventStore = eventStore;
            _tasks = new ConcurrentDictionary<Guid, PayloadTaskCompletionData>();
            ListenToEvents();
        }

        public Task<bool> Setup(Guid id, bool completeOnAcknowledge)
        {
            var tcs = new TaskCompletionSource<bool>();

            var data = new PayloadTaskCompletionData
            {
                CompleteOnAcknowledge = completeOnAcknowledge,
                TaskCompletionSource = tcs
            };

            _tasks[id] = data;

            return tcs.Task;
        }

        private void ListenToEvents()
        {
            _eventStore.OnResult += ev =>
            {
                if (_tasks.TryRemove(ev.Id, out var data))
                    switch (ev.EventType)
                    {
                        case SendEventType.Ack:
                            data.OnAcknowledgeResult(true);
                            break;
                        case SendEventType.Nack:
                            data.OnAcknowledgeResult(false);
                            break;
                        case SendEventType.Sent:
                            data.OnSendResult(true);
                            break;
                        case SendEventType.Failed:
                            data.OnSendResult(false);
                            break;
                    }
            };
        }
    }
}