using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Core.SessionPolicy
{
    public class DefaultSessionPolicy : ISessionPolicy
    {
        private readonly Queue _pendingGetNextAvailableSendQueueTasks;
        private readonly List<ISendQueue> _sendQueueList;
        private readonly ReaderWriterLockSlim _wrLock;

        private int _currentIndex;

        public DefaultSessionPolicy()
        {
            _pendingGetNextAvailableSendQueueTasks = new();
            _sendQueueList = new();
            _wrLock = new();
        }

        public void AddSendQueue(ISendQueue sendQueue)
        {
            try
            {
                _wrLock.EnterWriteLock();

                if (_sendQueueList.Any(sq => sq.Id == sendQueue.Id))
                {
                    throw new Exception("Added SendQueue already exists");
                }

                _sendQueueList.Add(sendQueue);
                
                sendQueue.OnAvailable += OnSendQueueAvailabilityChanged;
                
                OnSendQueueAvailabilityChanged(sendQueue.AvailabilityTicket);
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }
        }

        public void RemoveSendQueue(Guid sendQueueId)
        {
            try
            {
                _wrLock.EnterWriteLock();

                var sendQueueToBeRemoved = _sendQueueList.FirstOrDefault(sq => sq.Id == sendQueueId);

                if (sendQueueToBeRemoved is not null)
                {
                    _sendQueueList.Remove(sendQueueToBeRemoved);

                    sendQueueToBeRemoved.OnAvailable -= OnSendQueueAvailabilityChanged;
                }
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }
        }

        public Task<ISendQueue> GetNextAvailableSendQueueAsync(CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<ISendQueue>(TaskCreationOptions.RunContinuationsAsynchronously);

            CheckSendQueueAvailability(taskCompletionSource);
            
            return taskCompletionSource.Task;
        }

        private void OnSendQueueAvailabilityChanged(SendQueueAvailabilityTicket availabilityTicket)
        {
            if (_pendingGetNextAvailableSendQueueTasks.Count == 0)
            {
                return;
            }

            if (!availabilityTicket.IsAvailable)
            {
                return;
            }

            try
            {
                var dequeuedObject = _pendingGetNextAvailableSendQueueTasks.Dequeue();

                if (dequeuedObject is TaskCompletionSource<ISendQueue> pendingGetNextAvailableSendQueueTask)
                {
                    availabilityTicket.ReserveAvailability();

                    var sendQueue = _sendQueueList.First(sql => sql.Id == availabilityTicket.SendQueueId);
                    
                    pendingGetNextAvailableSendQueueTask.SetResult(sendQueue);
                }
            }
            catch (InvalidOperationException)
            {
                // do nothing
            }
        }

        private void CheckSendQueueAvailability(TaskCompletionSource<ISendQueue> pendingGetNextAvailableSendQueueTask)
        {
            // for each send queue check if any is available
            foreach (var sendQueue in _sendQueueList)
            {
                if (sendQueue.IsAvailable)
                {
                    pendingGetNextAvailableSendQueueTask.SetResult(sendQueue);
                    return;
                }
            }
            
            // if no send queue is available or non of current send queues are available
            // add the pending task to the list of pending task for future processing
            _pendingGetNextAvailableSendQueueTasks.Enqueue(pendingGetNextAvailableSendQueueTask);
        }

        public void Dispose()
        {
            foreach (var sendQueue in _sendQueueList)
            {
                sendQueue.OnAvailable -= OnSendQueueAvailabilityChanged;
            }
        }
    }
}