using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBroker.Socket.SocketWrapper
{
    public class TcpSocket: ITcpSocket
    {

        private readonly System.Net.Sockets.Socket _socket;

        public TcpSocket(System.Net.Sockets.Socket socket)
        {
            _socket = socket;
        }
        
        public void Close()
        {
            _socket.Close();
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