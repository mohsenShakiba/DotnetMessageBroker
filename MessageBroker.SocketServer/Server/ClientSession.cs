using MessageBroker.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public class ClientSession 
    {
        private bool _connected;
        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _sendEventArgs = new();
        private readonly SocketAsyncEventArgs _receiveEventArgs = new();

        private byte[] _msgHeaderBuf;
        private byte[] _msgBuf;
        private bool _receivingMsgHeader;

        public ClientSession(Socket socket)
        {
            _connected = true;
            _receiveEventArgs.Completed += OnReceiveCompleted;
            _sendEventArgs.Completed += OnSendCompleted;
            _socket = socket;

            _msgHeaderBuf = new byte[4];
            _msgBuf = new byte[1024];

            ReceiveHeader();
        }

        public void ReceiveHeader()
        {
            if (!_connected)
                return;

            _receivingMsgHeader = true;
            _receiveEventArgs.SetBuffer(_msgHeaderBuf);
            if (!_socket.ReceiveAsync(_receiveEventArgs))
            {
                OnReceiveCompleted(null, _receiveEventArgs);
            }
        }

        private void OnReceiveCompleted(object _, SocketAsyncEventArgs args)
        {
            var size = args.BytesTransferred;

            if (size == 0)
            {
                Close();
                return;
            }


            if (_receivingMsgHeader)
            {
                var msgSeize = BitConverter.ToInt32(_msgHeaderBuf);
                ReceiveMsg(msgSeize);
            }
            else
            {
                var msg = _msgBuf.AsSpan(0, size);
                OnReceived(msg);
            }
        }

        private void ReceiveMsg(int msgSize)
        {
            if (!_connected)
                return;

            if (msgSize != 14)
                Console.WriteLine("msg size is {0}", msgSize);

            _receivingMsgHeader = true;
            var msgSizeFixed = msgSize > _msgBuf.Length ? _msgBuf.Length : msgSize;
            _receiveEventArgs.SetBuffer(_msgBuf, 0, msgSizeFixed);
            _receivingMsgHeader = false;
            if (!_socket.ReceiveAsync(_receiveEventArgs))
            {
                OnReceiveCompleted(null, _receiveEventArgs);
            }
        }

        private void OnSendCompleted(object _, SocketAsyncEventArgs args)
        {
            if (!_connected)
                return;
        }

        protected void OnReceived(Span<byte> buff)
        {
            var msg = Encoding.UTF8.GetString(buff);
            Console.WriteLine("message is {0}", msg);
        }

        public void Close()
        {
            _connected = false;

            _sendEventArgs.Completed -= OnSendCompleted;
            _receiveEventArgs.Completed -= OnReceiveCompleted;

            _socket.Close();
            _socket.Dispose();
        }

    }
}
