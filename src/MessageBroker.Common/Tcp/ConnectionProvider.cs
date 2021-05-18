using System.Net;

namespace MessageBroker.Common.Tcp
{
    /// <summary>
    /// Provider used by ISocketServer to get IPEndpoint
    /// </summary>
    public class ConnectionProvider
    {
        public IPEndPoint IpEndPoint { get; set; }
    }
}