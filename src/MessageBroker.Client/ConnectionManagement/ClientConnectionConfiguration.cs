using System.Net;

namespace MessageBroker.Client.ConnectionManagement
{
    /// <summary>
    /// Configuration for connecting to broker server
    /// </summary>
    public class ClientConnectionConfiguration
    {
        /// <summary>
        /// Endpoint that is used for connecting to server
        /// </summary>
        public EndPoint EndPoint { get; set; }

        /// <summary>
        /// If true, once the connection fails it tries to reconnect to it
        /// </summary>
        public bool AutoReconnect { get; set; }
    }
}