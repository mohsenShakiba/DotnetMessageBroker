using System;
using System.Net;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public interface ISocketServer
    {
        void Start(IPEndPoint endpoint);
        void Stop();
        void Send(Guid sessionId, byte[] payload);
    }
}