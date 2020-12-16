using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public interface IClientSession
    {

        Guid SessionId { get; }

        void SendSync(byte[] payload);
        void Send(byte[] payload);
        void Close();
        void Dispose();
    }
}
