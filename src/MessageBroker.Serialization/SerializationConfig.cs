namespace MessageBroker.Serialization
{
    public class SerializationConfig
    {
        public int MessageHeaderSize { get; init; }
        public int StartMessageSize { get; init; }
        public int MaxBodySize { get; init; }


        public static SerializationConfig Default => new()
        {
            MaxBodySize = 1024 * 1024,
            StartMessageSize = 128,
            MessageHeaderSize = 4
        };
    }
}