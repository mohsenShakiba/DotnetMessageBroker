using System;

namespace MessageBroker.Core.PayloadProcessing
{
    /// <summary>
    /// Will deserialize and process data received by the client
    /// </summary>
    /// <remarks>Any data received by the client will be dispatch to <see cref="OnDataReceived" /></remarks>
    public interface IPayloadProcessor
    {
        /// <summary>
        /// Called by <see cref="Broker" /> when a new payload has been received and it needs to be dispatch and processed
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="data"></param>
        void OnDataReceived(Guid sessionId, Memory<byte> data);
    }
}