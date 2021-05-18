using System.Net;

namespace MessageBroker.Client.ConnectionManagement
{
    /// <summary>
    /// Configuration for connecting to broker server
    /// </summary>
    public class ClientConnectionConfiguration
    {
        public EndPoint EndPoint { get; set; }
        public bool AutoReconnect { get; set; }
    }
}