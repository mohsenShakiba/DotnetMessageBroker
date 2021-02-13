using System.Net;

namespace MessageBroker.Socket.Server
{
    public interface ISocketServer
    {
        void Start(IPEndPoint endpoint);
        void Stop();
    }
}