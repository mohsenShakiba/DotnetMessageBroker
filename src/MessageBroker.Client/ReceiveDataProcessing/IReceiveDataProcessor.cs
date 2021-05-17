using System;
using System.Diagnostics;
using MessageBroker.Core.Clients;
using MessageBroker.TCP;
using MessageBroker.TCP.EventArgs;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    /// <summary>
    /// Will process data received from <see cref="IClient"/>
    /// </summary>
    public interface IReceiveDataProcessor
    {
        /// <summary>
        /// Called when payload data is received from <see cref="IClient"/>
        /// </summary>
        /// <param name="clientSessionObject">Sender</param>
        /// <param name="dataReceivedEventArgs">Event args for when payload data is received</param>
        void DataReceived(object clientSessionObject, ClientSessionDataReceivedEventArgs dataReceivedEventArgs);

        event Action<Guid> OnOkReceived;
        event Action<Guid, string> OnErrorReceived;
    }
}