using System;
using MessageBroker.TCP;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    public interface IReceiveDataProcessor : ISocketDataProcessor
    {
        event Action OnReadyReceived;
    }
}