using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface ISessionEventListener
    {
        void OnReceived(Guid sessionId, Memory<byte> data);
        void OnSessionDisconnected(Guid sessionId);
    }
}
