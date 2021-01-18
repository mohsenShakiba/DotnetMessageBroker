using MessageBroker.SocketServer.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
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
        private readonly ILogger<ClientSession> _logger;

        private byte[] _receiveBuff;
        private byte[] _sendBuff;
        private bool _isSending;
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
            
            SetupEventArgs();

            SetupReceiveBufferWithSize();
            SetupSendBufferWithSize();

            Receive();
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
            // get the transferred size
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
            var msgSize = BitConverter.ToInt32(_receiveBuff.AsSpan(0, _config.DefaultHeaderSize));

            // receive exactly the size converted from the last step
            ReceiveMsg(msgSize);
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

            if (msgSize > _receiveBuff.Length - _config.DefaultHeaderSize)
                SetupReceiveBufferWithSize(msgSize);

            _receiveEventArgs.SetBuffer(_receiveBuff, 0, msgSize);

            if (!_socket.ReceiveAsync(_receiveEventArgs))
                OnMessageReceived(null, _receiveEventArgs);
        }

        private void OnMessageReceived(object _, SocketAsyncEventArgs args)
        {
            // get the transferred size
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

        /// <summary>
        /// sets an action for when send async finished sending data
        /// </summary>
        /// <param name="onSendCompleted"></param>
        public void SetupSendCompletedHandler(Action onSendCompleted)
        {
            _sendEventArgs.Completed += (_, _) =>
            {
                onSendCompleted();
            };
        }

        /// <summary>
        /// Send is synchronous and will block until the message is sent
        /// this method is only used for testing
        /// </summary>
        /// <param name="payload"></param>
        public void Send(Memory<byte> payload)
        {
            _socket.Send(payload.Span);
        }

        /// <summary>
        /// SendAsync will asynchronously send the message, then it will call the complete handler
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool SendAsync(Memory<byte> payload)
        {
            // if (payload.Length > _sendBuff.Length)
            //     SetupSendBufferWithSize(payload.Length);
            //
            // payload.CopyTo(_sendBuff);
            
            _sendEventArgs.SetBuffer(payload);
            
            return _socket.SendAsync(_sendEventArgs);
        }

        #endregion

        /// <summary>
        /// OnReceive is called when the a message is successfully received
        /// </summary>
        /// <param name="buff"></param>
        private void OnReceived(Memory<byte> buff)
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

        /// <summary>
        /// SetupReceiveBufferWithSize is called when the size of message to be received is larger than the current buffer
        /// so we need to increase the size of buffer 
        /// </summary>
        /// <param name="desiredSize"></param>
        private void SetupReceiveBufferWithSize(int? desiredSize = null)
        {
            var size = desiredSize ?? _config.DefaultBodySize;
            var newBuffer = ArrayPool<byte>.Shared.Rent(size + _config.DefaultHeaderSize);
            if (_receiveBuff != null)
            {
                ArrayPool<byte>.Shared.Return(_receiveBuff);
            }
            _receiveBuff = newBuffer;
        }
        
        private void SetupSendBufferWithSize(int? desiredSize = null)
        {
            var size = desiredSize ?? _config.DefaultBodySize;
            var newBuffer = ArrayPool<byte>.Shared.Rent(size);
            if (_sendBuff != null)
            {
                ArrayPool<byte>.Shared.Return(_sendBuff);
            }
            _sendBuff = newBuffer;
        }
    
    }
}
