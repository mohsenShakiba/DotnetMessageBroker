using MessageBroker.SocketServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Service
{
    public interface IMessageQueue
    {
        ValueTask Push(MessagePayload msg);
        ValueTask<bool> TryPop(out MessagePayload msg);
    }
}
