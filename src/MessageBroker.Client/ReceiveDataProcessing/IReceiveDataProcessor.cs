using System;
using MessageBroker.Common.Models;
using MessageBroker.Common.Tcp.EventArgs;
using MessageBroker.Core.Clients;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    /// <summary>
    /// Will process data received from <see cref="IClient" />
    /// </summary>
    public interface IReceiveDataProcessor
    {
        /// <summary>
        /// Called when payload data is received from <see cref="IClient" />
        /// </summary>
        /// <param name="clientSessionObject">Sender</param>
        /// <param name="dataReceivedEventArgs">Event args for when payload data is received</param>
        void DataReceived(object clientSessionObject, ClientSessionDataReceivedEventArgs dataReceivedEventArgs);

        /// <summary>
        /// Invoked when a new <see cref="Ok" /> is received from server
        /// </summary>
        event Action<Guid> OnOkReceived;

        /// <summary>
        /// Invoked when a new <see cref="Error" /> is received from server
        /// </summary>
        event Action<Guid, string> OnErrorReceived;
    }
}