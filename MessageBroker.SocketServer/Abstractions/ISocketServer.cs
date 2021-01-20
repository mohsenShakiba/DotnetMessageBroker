using System.Net;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface ISocketServer
    {
        void Start(IPEndPoint endpoint);
        void Stop();
    }
}