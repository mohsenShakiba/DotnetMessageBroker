namespace MessageBroker.TCP.EventArgs
{
    /// <summary>
    /// Event args for when socket server accepts a new connection
    /// </summary>
    public class SocketAcceptedEventArgs: System.EventArgs
    {
        public ITcpSocket Socket { get; init; }
    }
}