using System.Net.Sockets;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.ConnectionManager
{
    public interface IConnectionManager
    {
        bool IsConnected { get; }
        string LastSocketError { get; }
        Socket Socket { get; }
        
        void Connect(SocketConnectionConfiguration configuration);
        void CheckConnectionStatusAndRetryIfDisconnected();
        void Disconnect();
    }
}