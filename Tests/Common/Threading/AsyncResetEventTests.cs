using System.Threading.Tasks;
using MessageBroker.Common.Threading;
using Xunit;

namespace Tests.Common.Threading
{
    public class AsyncResetEventTests
    {

        [Fact]
        public async Task Unblock_WhenBlocked_IsUnblocked()
        {
            var are = new AsyncResetEvent();
            are.Block();

            _ = Task.Factory.StartNew(() =>
            {
                Task.Delay(1000);
                are.UnBlock();
            });

            await are.WaitAsync();
        }

        [Fact]
        public async Task Block_WhenNotBlocked_IsBlocked()
        {
            var are = new AsyncResetEvent();
            are.Block();

            var result = are.WaitAsync();

            await Task.Delay(100);
            
            Assert.False(result.IsCompleted);
        }
    }
}