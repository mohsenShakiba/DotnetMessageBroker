using System;

namespace MessageBroker.Core
{
    public class Publisher
    {
        public Guid SessionId { get; private set; }

        public Publisher(Guid sessionId)
        {
            SessionId = sessionId;
        }
    }
}
