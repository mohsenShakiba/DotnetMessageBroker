using System;

namespace MessageBroker.Core.PayloadProcessing
{
    public interface IPayloadProcessor
    {
        void OnDataReceived(Guid sessionId, Memory<byte> data);
    }
}