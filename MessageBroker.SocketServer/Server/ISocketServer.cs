using System;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public interface ISocketServer
    {
        void Start();
        void Stop();

    }
}