using System;
using System.Threading.Tasks;

namespace MessageBroker.Socket.SocketWrapper
{
    public interface ITcpSocket
    {
        void Close();

        ValueTask<int> SendAsync(Memory<byte> data);
        ValueTask<int> ReceiveAsync(Memory<byte> buffer);
    }
}