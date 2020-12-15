using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public interface IClientSession
    {
        void Send(byte[] payload);
        void Close();
        void Dispose();
    }
}
