using System;

namespace MessageBroker.TCP.EventArgs
{
    /// <summary>
    /// Event args for when payload data is received from client
    /// </summary>
    public class ClientSessionDataReceivedEventArgs
    {
        public Guid Id { get; init; }
        public Memory<byte> Data { get; init; }
    }
}