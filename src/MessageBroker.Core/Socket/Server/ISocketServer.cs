using System.Net;

namespace MessageBroker.Core.Socket.Server
{
    public interface ISocketServer
    {
        void Start(IPEndPoint endpoint);
        void Stop();
    }
}