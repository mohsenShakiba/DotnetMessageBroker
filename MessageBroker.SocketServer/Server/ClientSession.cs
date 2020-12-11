using MessageBroker.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public class ClientSession : IDisposable
    {
        private TcpSocketServer _server;
        private readonly Socket _socket;
        private readonly SessionConfiguration _config;
        private readonly SocketAsyncEventArgs _sendEventArgs;
        private readonly SocketAsyncEventArgs _receiveEventArgs;
        private readonly SocketAsyncEventArgs _sizeEventArgs;
        private readonly AutoResetEvent _receiveResetEvent;
        private readonly AutoResetEvent _sendResetEvent;
        private readonly ILogger<ClientSession> _logger;
        private readonly Guid _sessionId;

        private byte[] _receiveBuff;
        private bool _connected;

        public ClientSession(TcpSocketServer server, Socket socket, SessionConfiguration config, ILogger<ClientSession> logger)
        {
            _logger = logger;
            _server = server;
            _socket = socket;
            _config = config;

            _connected = true;
            _sessionId = new();

            _sendEventArgs = new();
            _receiveEventArgs = new();
            _sizeEventArgs = new();

            _receiveResetEvent = new(false);
            _sendResetEvent = new(true);

            SetupEventArgs();

            SetupBuffers();

            Receive();
        }

        private void SetupBuffers()
        {
            _receiveBuff = new byte[_config.DefaultHeaderSize + _config.DefaultMaxBodySize];
        }

        private void IncreaseReceiveBuffer(int size)
        {
            _config.DefaultMaxBodySize = size;
            SetupBuffers();
        }

        private void SetupEventArgs()
        {
            _sizeEventArgs.Completed += OnMessageSizeReceived;
            _receiveEventArgs.Completed += OnMessageReceived;
            _sendEventArgs.Completed += OnSendCompleted;
        }

        /// <summary>
        /// this method will start recieving message from socket
        /// </summary>
        private void Receive()
        {
            Task.Factory.StartNew(() =>
            {
                while (_connected)
                {
                    ReceiveMessageSize();

                    _receiveResetEvent.WaitOne();
                }
            }, TaskCreationOptions.LongRunning);
        }

        #region ReceiveSize

        /// <summary>
        /// this method will read only the 4 bytes of the message to know how long the actual incoming message is.
        /// it will then call receive message method once the size in known
        /// </summary>
        public void ReceiveMessageSize()
        {
            if (!_connected)
                return;

            // resetting the _receiveEventArgs
            _sizeEventArgs.SetBuffer(_receiveBuff, default, _config.DefaultHeaderSize);

            // receive the 4 bytes from docket 
            if (!_socket.ReceiveAsync(_sizeEventArgs))
            {
                OnMessageSizeReceived(null, _sizeEventArgs);
            }
        }

        private void OnMessageSizeReceived(object _, SocketAsyncEventArgs args)
        {
            // get the transfered size
            var size = args.BytesTransferred;

            // if operation fails
            if (args.SocketError != SocketError.Success)
            {
                _logger.LogError($"the receive operation failed with error {args.SocketError}");
                Close();
                return;
            }

            // check if size of header is invalid, close the connection
            if (size != _config.DefaultHeaderSize)
            {
                _logger.LogError("failed to read the message size, removing session");
                Close();
                return;
            }

            // convert the first 4 bytes of the buffer to int32
            var msgSeize = BitConverter.ToInt32(_receiveBuff.AsSpan(0, _config.DefaultHeaderSize));

            // receive exactly the size converted from the last step
            ReceiveMsg(msgSeize);
        }

        #endregion

        #region ReceiveMessage

        /// <summary>
        /// this method will read exactly n bytes from socket
        /// </summary>
        /// <param name="msgSize">the size of message to read</param>
        private void ReceiveMsg(int msgSize)
        {
            if (msgSize <= 0)
            {
                _logger.LogError("invalid message size, closing the connection");
                Close();
                return;
            }

            if (!_connected)
                return;

            if (msgSize > _config.DefaultMaxBodySize)
                IncreaseReceiveBuffer(msgSize);

            _receiveEventArgs.SetBuffer(_receiveBuff, 0, msgSize);

            if (!_socket.ReceiveAsync(_receiveEventArgs))
                OnMessageReceived(null, _receiveEventArgs);
        }

        private void OnMessageReceived(object _, SocketAsyncEventArgs args)
        {
            // get the transfered size
            var size = args.BytesTransferred;

            // if operation fails
            if (args.SocketError != SocketError.Success)
            {
                _logger.LogError($"the receive operation failed with error {args.SocketError}");
                Close();
                return;
            }

            // check if size is invalid, close the connection
            if (size <= 0)
            {
                _logger.LogError("failed to read the message body, removing session");
                Close();
                return;
            }

            OnReceived(_receiveBuff.AsMemory(0, size));

            // signal the receive method to continue
            _receiveResetEvent.Set();
        }

        #endregion

        #region Send    

        public void Send(byte[] payload)
        {
            _sendResetEvent.WaitOne();

            _sendEventArgs.SetBuffer(payload);

            if (!_socket.SendAsync(_sendEventArgs))
                OnSendCompleted(null, _sendEventArgs);

        }

        private void OnSendCompleted(object _, SocketAsyncEventArgs args)
        {
            if (!_connected)
                return;

            if (args.SocketError != SocketError.Success)
            {
                _logger.LogError($"the send operation failed with error {args.SocketError}");
                Close();
                return;
            }

            _sendResetEvent.Set();
        }

        #endregion

        protected void OnReceived(Memory<byte> buff)
        {
            _logger.LogInformation($"received {buff.Length} from client");

            _server.OnReceived(_sessionId, buff);
        }

        public void Close()
        {
            _server = null;
            _connected = false;

            _sendEventArgs.Completed -= OnSendCompleted;
            _receiveEventArgs.Completed -= OnMessageReceived;
            _sizeEventArgs.Completed -= OnMessageSizeReceived;

            _socket.Close();
            _socket.Dispose();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
