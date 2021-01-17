namespace MessageBroker.Core.Configurations
{
    public class BaseConfiguration
    {
        public int MessageHeaderSize { get; init; }
        public int StartMessageSize { get; init; }
        public int MaxBodySize { get; init; }


        public static BaseConfiguration Default => new BaseConfiguration
        {
            MaxBodySize = 1024 * 1024,
            StartMessageSize = 128,
            MessageHeaderSize = 4
        };
    }
}