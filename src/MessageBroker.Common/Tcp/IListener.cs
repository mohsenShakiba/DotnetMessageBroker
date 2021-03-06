﻿using System;
using MessageBroker.Common.Tcp.EventArgs;

namespace MessageBroker.Common.Tcp
{
    /// <summary>
    /// TcpServer that listens on IpEndPoint and accepts the incoming connections
    /// once a connection is accepted the <see cref="OnSocketAccepted" /> event is called
    /// </summary>
    /// <seealso cref="TcpListener" />
    public interface IListener : IDisposable
    {
        /// <summary>
        /// Event for when a socket connection has been accepted
        /// </summary>
        event EventHandler<SocketAcceptedEventArgs> OnSocketAccepted;

        /// <summary>
        /// Will bind to endpoint and start listening to incoming connections
        /// </summary>
        /// <exception cref="InvalidOperationException">Server is already started</exception>
        void Start();

        /// <summary>
        /// Will stop and dispose the server
        /// </summary>
        /// <remarks>>All the sessions will be disconnected and removed</remarks>
        /// <exception cref="ObjectDisposedException">The server has been previously disposed</exception>
        void Stop();
    }
}