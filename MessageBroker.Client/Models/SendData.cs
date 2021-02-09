using System;

namespace MessageBroker.Client.Models
{
    public class SendData
    {
        public Guid Id { get; set; }
        public Memory<byte> Data { get; set; }
    }
}