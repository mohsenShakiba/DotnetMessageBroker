using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.SocketServer.Models
{
    public record SocketClient(Guid SessionId)
    {
    }
}
