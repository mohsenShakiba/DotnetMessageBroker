using System;
using MessageBroker.Client.Models;
using MessageBroker.Common.Binary;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    public interface IReceiveDataProcessor
    {
        public void AddReceiveDataChunk(Memory<byte> binaryChunk);
    }
}