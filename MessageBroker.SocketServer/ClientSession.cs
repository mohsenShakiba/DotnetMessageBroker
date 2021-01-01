using MessageBroker.SocketServer.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer
{
    /// <summary>
    /// ClientSession stores information about the accepted socket
    /// it will continue to receive data from socket and allows sending data to socket
    /// </summary>
    public class ClientSession : IClientSession, IDisposable
    {
        public Guid SessionId { get; }

        private ISessionEventListener _eventListener;
        private readonly Socket _socket;
        private readonly SessionConfiguration _config;
        private readonly SocketAsyncEventArgs _sendEventArgs;
        private readonly SocketAsyncEventArgs _receiveEventArgs;
        private readonly SocketAsyncEventArgs _sizeEventArgs;
        private readonly AutoResetEvent _receiveResetEvent;
        private readonly AutoResetEvent _sendResetEvent;
        private readonly ILogger<ClientSession> _logger;

        private byte[] _receiveBuff;
        private bool _connected;

        public ClientSession(ISessionEventListener eventListener, Socket socket, SessionConfiguration config, ILogger<ClientSession> logger)
        {
            _logger = logger;
            _eventListener = eventListener;
            _socket = socket;
            _config = config;

            _connected = true;
            SessionId = Guid.NewGuid();

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
        private void ReceiveMessageSize()
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

            var sizeToReceive = msgSize > _receiveBuff.Length ? _receiveBuff.Length : msgSize;

            _receiveEventArgs.SetBuffer(_receiveBuff, 0, sizeToReceive);

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

        public void SetupSendCompletedHandler(Action onSendCompleted)
        {
            _sendEventArgs.Completed += (_, _) =>
            {
                onSendCompleted();
            };
        }

        public void Send(Memory<byte> payload)
        {
            _socket.Send(payload.Span);
        }

        public bool SendAsync(Memory<byte> payload)
        {
            _sendEventArgs.SetBuffer(payload);
            return _socket.SendAsync(_sendEventArgs);
        }


        #endregion

        protected void OnReceived(Memory<byte> buff)
        {
            _logger.LogInformation($"received {buff.Length} from client");
            _eventListener.OnReceived(SessionId, buff);

        }

        public void Close()
        {
            _connected = false;
            _receiveEventArgs.Completed -= OnMessageReceived;
            _sizeEventArgs.Completed -= OnMessageSizeReceived;

            _socket.Close();
            _socket.Dispose();

            _eventListener.OnSessionDisconnected(SessionId);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
