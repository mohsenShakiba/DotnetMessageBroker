using System;

namespace MessageBroker.Core.PayloadProcessing
{
    /// <summary>
    /// Will deserialize and process data received by the client
    /// </summary>
    /// <remarks>Any data received by the client will be dispatch to <see cref="OnDataReceived" /></remarks>
    public interface IPayloadProcessor
    {
        void OnDataReceived(Guid sessionId, Memory<byte> data);
    }
}