using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
