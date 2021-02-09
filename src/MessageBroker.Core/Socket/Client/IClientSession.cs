using System;
using System.Threading.Tasks;

namespace MessageBroker.Core.Socket.Client
{
    public interface IClientSession
    {
        Guid Id { get; }
        Task<bool> SendAsync(Memory<byte> payload);
        void Close();
    }

}