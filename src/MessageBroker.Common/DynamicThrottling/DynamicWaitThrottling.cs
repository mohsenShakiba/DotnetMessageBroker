using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Common.DynamicThrottling
{
    /// <summary>
    /// Will cause threads to be asynchronously delayed to prevent starvation
    /// usage:
    /// imagine we have a for loop that checks for something to be asynchronously available
    /// if the loop runs without any delay, then we are wasting processing power
    /// if we specify a fixed delay such as 100 ms then it might be too much or too little
    /// what this object does instead is to wait very little for the first few times, and continuously
    /// increase the delay dynamically
    /// so we might start with 1ms then 2ms, 4ms and so on
    /// </summary>
    public class DynamicWaitThrottling
    {
        public DynamicWaitThrottling(int baseDelay = 1, int multiplier = 4, int maxDelay = 100)
        {
            BaseDelay = baseDelay;
            CurrentDelay = baseDelay;
            Multiplier = multiplier;
            MaxDelay = maxDelay;
        }

        public int MaxDelay { get; }
        public int Multiplier { get; }
        public int BaseDelay { get; }
        public int CurrentDelay { get; private set; }

        public Task WaitAsync(CancellationToken? cancellationToken = null)
        {
            return Task.Delay(CurrentDelay, cancellationToken ?? CancellationToken.None);
        }

        public Task WaitAndIncrease(CancellationToken? cancellationToken = null)
        {
            try
            {
                return WaitAsync(cancellationToken);
            }
            finally
            {
                Increase();
            }
        }

        public void Increase()
        {
            if (CurrentDelay * Multiplier > MaxDelay)
            {
                // do nothing
            }
            else
            {
                CurrentDelay *= Multiplier;
            }
        }

        public void Reset()
        {
            CurrentDelay = BaseDelay;
        }
    }
}