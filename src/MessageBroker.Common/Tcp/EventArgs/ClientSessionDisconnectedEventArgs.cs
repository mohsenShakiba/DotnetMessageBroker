using System;

namespace MessageBroker.Common.Tcp.EventArgs
{
    /// <summary>
    /// Event args for when client socket is disconnected
    /// </summary>
    public sealed class ClientSessionDisconnectedEventArgs : System.EventArgs
    {
        public Guid Id { get; set; }
    }
}