using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Messages
{
    public record RegisterSubscriber(Guid SessionId);
}
