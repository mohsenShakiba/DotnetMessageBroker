using System.Net;

namespace MessageBroker.TCP.Server
{
    public interface ISocketServer
    {
        void Start(IPEndPoint endpoint);
        void Stop();
    }
}