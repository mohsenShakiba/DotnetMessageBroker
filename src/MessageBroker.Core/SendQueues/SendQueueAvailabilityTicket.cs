using System;

namespace MessageBroker.Core
{
    public class SendQueueAvailabilityTicket
    {
        public Guid SendQueueId { get; }
        public int AvailabilityCount { get; private set; }
        public bool IsAvailable => AvailabilityCount > 0;

        private object _lock;

        public SendQueueAvailabilityTicket(Guid sendQueueId)
        {
            SendQueueId = sendQueueId;
            _lock = new();
        }

        public void SetAvailability(int availabilityCount)
        {
            AvailabilityCount = availabilityCount;
        }
        
        public void ReserveAvailability()
        {
            lock (_lock)
            {
                if (AvailabilityCount <= 0)
                {
                    throw new InvalidOperationException("The ticket has no more availability to be reserved");
                }
                
                AvailabilityCount -= 1;
            }
        }
    }
}