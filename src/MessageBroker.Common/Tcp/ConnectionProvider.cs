using System.Net;

namespace MessageBroker.TCP
{
    /// <summary>
    /// Provider used by ISocketServer to get IPEndpoint
    /// </summary>
    public class ConnectionProvider
    {
        public IPEndPoint IpEndPoint { get; init; }
    }
}