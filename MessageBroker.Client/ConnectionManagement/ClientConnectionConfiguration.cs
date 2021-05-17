using System.Net;

namespace MessageBroker.Client.ConnectionManagement
{
    /// <summary>
    /// Configuration for connecting to broker server
    /// </summary>
    public class ClientConnectionConfiguration
    {
        public IPEndPoint IpEndPoint { get; init; }
        public bool AutoReconnect { get; init; }
    }
}