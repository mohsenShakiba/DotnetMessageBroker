using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Common.Threading
{
    public sealed class AsyncResetEvent
    {
        private bool _isBlocked;
        private TaskCompletionSource _tcs;

        public Task WaitAsync()
        {
            if (!_isBlocked)
            {
                return Task.CompletedTask;
            }

            if (_tcs != null)
                throw new Exception($"{nameof(AsyncResetEvent)} is called in multiple threads");

            _tcs = new TaskCompletionSource();
            
            return _tcs.Task;
        }

        public void Block()
        {
            _isBlocked = true;
        }

        public void UnBlock()
        {
            _isBlocked = false;
            _tcs?.SetResult();
            _tcs = null;
        }
    }
}