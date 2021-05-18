namespace MessageBroker.Common.Tcp.EventArgs
{
    /// <summary>
    /// Event args for when socket server accepts a new connection
    /// </summary>
    public class SocketAcceptedEventArgs : System.EventArgs
    {
        public ISocket Socket { get; set; }
    }
}