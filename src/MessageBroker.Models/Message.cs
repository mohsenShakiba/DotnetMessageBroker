using System;

namespace MessageBroker.Models
{
    /// <summary>
    ///     the message contains data that is sent by publisher and received by subscriber
    /// </summary>
    public ref struct Message
    {
        public Guid Id { get; set; }
        public string Route { get; init; }
        public Memory<byte> Data { get; init; }
        public byte[] OriginalMessageData { get; init; }

        public void SetNewId()
        {
            Id = Guid.NewGuid();
        }
        
    }
}