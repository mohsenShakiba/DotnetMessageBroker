using System.Net;

namespace MessageBroker.Client.Models
{
    public class SocketConnectionConfiguration
    {
        public IPEndPoint IpEndPoint { get; init; }
        public bool RetryOnFailure { get; init; }
    }
}