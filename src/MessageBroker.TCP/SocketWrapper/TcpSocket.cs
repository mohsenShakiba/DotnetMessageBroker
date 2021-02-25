using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBroker.TCP.SocketWrapper
{
    public class TcpSocket: ITcpSocket
    {

        private readonly Socket _socket;

        public TcpSocket(Socket socket)
        {
            _socket = socket;
        }

        public bool Connected => _socket.Connected;

        public void Close()
        {
            _socket.Close();
        }

        public void Connect(IPEndPoint ipEndPoint)
        {
            _socket.Connect(ipEndPoint);
        }

        public void Disconnect(bool reuseSocket)
        {
            _socket.Disconnect(reuseSocket);
        }

        public ValueTask<int> SendAsync(Memory<byte> data)
        {
            return _socket.SendAsync(data, SocketFlags.None);
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer)
        {
            return _socket.ReceiveAsync(buffer, SocketFlags.None);
        }
    }
}