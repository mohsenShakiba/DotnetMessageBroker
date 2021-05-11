using System;

namespace MessageBroker.TCP.EventArgs
{
    /// <summary>
    /// Event args for when client socket is disconnected
    /// </summary>
    public sealed class ClientSessionDisconnectedEventArgs: System.EventArgs
    {
        public Guid Id { get; init; }
    }
}