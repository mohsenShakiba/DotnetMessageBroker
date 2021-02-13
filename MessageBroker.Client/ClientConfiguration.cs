namespace MessageBroker.Client
{
    public class ClientConfiguration
    {
        private static ClientConfiguration _configuration;

        public int SendMessageChannelSize { get; init; }
        public int ReceiveMessageChannelSize { get; init; }
        public int ReceiveDataBufferSize { get; init; }
        public int MaxSandRetryCount { get; init; }
        public int InitialReceiveBufferSize { get; init; }

        public static ClientConfiguration CurrentConfiguration => _configuration ?? new ClientConfiguration
        {
            ReceiveDataBufferSize = 1024,
            ReceiveMessageChannelSize = 1024,
            SendMessageChannelSize = 1024,
            InitialReceiveBufferSize = 1024,
            MaxSandRetryCount = 10
        };

        public static void SetConfiguration(ClientConfiguration configuration)
        {
            _configuration = configuration;
        }
    }
}