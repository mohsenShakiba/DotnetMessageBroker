namespace MessageBroker.SocketServer
{
    /// <summary>
    /// class that is used for configuring the behavior of session socket
    /// </summary>
    public class SessionConfiguration
    {
        /// <summary>
        /// size of the payload header, default to 4
        /// </summary>
        public int DefaultHeaderSize { get; set; }
        
        /// <summary>
        /// the default size used for receiving payloads, default to 128
        /// </summary>
        public int DefaultBodySize { get; set; }

        public static SessionConfiguration Default()
        {
            return new SessionConfiguration
            {
                DefaultHeaderSize = 4,
                DefaultBodySize = 128
            };
        }
    }
}
