using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Messages
{
    public enum PayloadType
    {
        Msg = 1,
        Ack = 2,
        Nack = 3,
        Listen = 4,
        Unlisten = 5,
        Subscribe = 6,
        QueueCreate = 7,
        QueueDelete = 8
    }
}
