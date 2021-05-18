using System;

namespace MessageBroker.Common.Tcp.EventArgs
{
    /// <summary>
    /// Event args for when payload data is received from client
    /// </summary>
    public class ClientSessionDataReceivedEventArgs
    {
        public Guid Id { get; set; }
        public Memory<byte> Data { get; set; }
    }
}