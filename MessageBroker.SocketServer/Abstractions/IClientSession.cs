using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface IClientSession
    {

        Guid SessionId { get; }

        void Send(byte[] payload);
        void SendAsync(byte[] payload);
        void Close();
        void Dispose();
    }
}
