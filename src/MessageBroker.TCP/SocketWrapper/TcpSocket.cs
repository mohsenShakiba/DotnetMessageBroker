using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBroker.TCP.SocketWrapper
{
    public class TcpSocket: ITcpSocket
    {

        private Socket _socket;

        public TcpSocket(Socket socket = null)
        {
            _socket = socket;
        }

        public bool Connected => _socket?.Connected ?? false;

        public void Close()
        {
            _socket.Close();
        }

        public void Connect(IPEndPoint ipEndPoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(ipEndPoint);
        }

        public void Disconnect(bool reuseSocket)
        {
            try
            {
                _socket.Disconnect(reuseSocket);
            }
            catch
            {
                // do nothing
            }
        }

        public async ValueTask<int> SendAsync(Memory<byte> data)
        {
            try
            {
                return await _socket.SendAsync(data, SocketFlags.None);
            }
            catch
            {
                return 0;
            }
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer)
        {
            try
            {
                return await _socket.ReceiveAsync(buffer, SocketFlags.None);
            }
            catch
            {
                return 0;
            }
        }
    }
}