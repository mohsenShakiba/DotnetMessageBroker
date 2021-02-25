using System;
using System.Net;
using System.Threading.Tasks;

namespace MessageBroker.TCP.SocketWrapper
{
    public interface ITcpSocket
    {
        bool Connected { get; }
        void Close();
        void Connect(IPEndPoint ipEndPoint);
        void Disconnect(bool reuseSocket);
        ValueTask<int> SendAsync(Memory<byte> data);
        ValueTask<int> ReceiveAsync(Memory<byte> buffer);
    }
}