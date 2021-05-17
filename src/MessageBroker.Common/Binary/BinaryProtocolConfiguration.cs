namespace MessageBroker.Common.Binary
{
    public static class BinaryProtocolConfiguration
    {
        /// <summary>
        /// Number of bytes used for storing the size of payload when transferring on wire 
        /// </summary>
        public const int PayloadHeaderSize = 4;
        
        /// <summary>
        /// Buffer size used to receive data from wire
        /// </summary>
        public const int ReceiveDataSize = 1024;

        /// <summary>
        /// Number of bytes used for storing int as binary
        /// </summary>
        public const int SizeForInt = 4;
        
        /// <summary>
        /// Number of bytes used for string Guid
        /// </summary>
        public const int SizeForGuid = 16;
        
        /// <summary>
        /// Number of bytes used for string new line `\n`
        /// </summary>
        public const int SizeForNewLine = 1;
    }
}