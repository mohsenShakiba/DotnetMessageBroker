using System;
using System.Net;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface ISocketServer
    {
        void Start(IPEndPoint endpoint);
        void Stop();
    }
}