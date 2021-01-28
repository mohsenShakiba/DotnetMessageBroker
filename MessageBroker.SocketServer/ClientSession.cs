using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
using MessageBroker.Serialization;
using MessageBroker.SocketServer.Abstractions;
using Microsoft.Extensions.Logging;

namespace MessageBroker.SocketServer
{
    /// <summary>
    /// ClientSession stores information about the accepted socket
    /// it will continue to receive data from socket and allows sending data to socket
    /// this class isn't thread safe but it's only used by send queue which takes care of multi threading
    /// </summary>
    public class ClientSession : IClientSession, IDisposable
    {
        private readonly SocketAsyncEventArgs _receiveEventArgs;
        private readonly AutoResetEvent _receiveResetEvent;
        private readonly SocketAsyncEventArgs _sendEventArgs;
        private readonly SocketAsyncEventArgs _sizeEventArgs;
        private readonly Socket _socket;
        private readonly ISessionEventListener _eventListener;

        private bool _connected;
        private byte[] _receiveBuff;
        private byte[] _sendBuff;
        
        private Action<Guid> _onSendCompletedHandler;
        private Action<Guid> _onSendFailedHandler;
        private Guid _sendPayloadId;
        
        public Guid SessionId { get; }

        public ClientSession(ISessionEventListener eventListener, Socket socket)
        {
            _eventListener = eventListener;
            _socket = socket;

            _connected = true;
            SessionId = Guid.NewGuid();

            _sendEventArgs = new SocketAsyncEventArgs();
            _receiveEventArgs = new SocketAsyncEventArgs();
            _sizeEventArgs = new SocketAsyncEventArgs();

            _receiveResetEvent = new AutoResetEvent(false);

            SetupEventArgs();

            SetupReceiveBufferWithSize();
            SetupSendBufferWithSize();
            
            Receive();
        }


        public void Close()
        {
            Logger.LogInformation($"stopping client session {SessionId}");
            
            _connected = false;
            _receiveEventArgs.Completed -= OnMessageReceiveCompleted;
            _sizeEventArgs.Completed -= OnMessageSizeReceiveCompleted;
            _sendEventArgs.Completed -= OnSendCompleted;

            _socket.Close();
            _socket.Dispose();

            _eventListener.OnSessionDisconnected(SessionId);
        }

        public void Dispose()
        {
            Close();
        }

        private void SetupEventArgs()
        {
            _sizeEventArgs.Completed += OnMessageSizeReceiveCompleted;
            _receiveEventArgs.Completed += OnMessageReceiveCompleted;
            _sendEventArgs.Completed += OnSendCompleted;
        }

        /// <summary>
        ///     this method will start recieving message from socket
        /// </summary>
        private void Receive()
        {
            Logger.LogInformation($"starting client session {SessionId}");
            
            Task.Factory.StartNew(() =>
            {
                while (_connected)
                {
                    ReceiveMessageSize();
                    _receiveResetEvent.WaitOne();
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        ///     OnReceive is called when the a message is successfully received
        /// </summary>
        /// <param name="buff"></param>
        private void OnReceived(Memory<byte> buff)
        {
            _eventListener.OnReceived(SessionId, buff);
        }

        private void SetupReceiveBufferWithSize(int? desiredSize = null)
        {
            var size = desiredSize ?? SerializationConfig.ReceivePayloadStartingBufferSize;
            var newBuffer = ArrayPool<byte>.Shared.Rent(size + SerializationConfig.PayloadHeaderSize);
            if (_receiveBuff != null) ArrayPool<byte>.Shared.Return(_receiveBuff);
            _receiveBuff = newBuffer;
        }

        private void SetupSendBufferWithSize(int? desiredSize = null)
        {
            var size = desiredSize ?? SerializationConfig.SendPayloadStartingBufferSize;
            var newBuffer = ArrayPool<byte>.Shared.Rent(size);
            if (_sendBuff != null) ArrayPool<byte>.Shared.Return(_sendBuff);
            _sendBuff = newBuffer;
        }

        #region ReceiveSize

        /// <summary>
        ///     this method will read only the 4 bytes of the message to know how long the actual incoming message is.
        ///     it will then call receive message method once the size in known
        /// </summary>
        private void ReceiveMessageSize()
        {
            if (!_connected)
                return;

            // resetting the _receiveEventArgs
            _sizeEventArgs.SetBuffer(_receiveBuff, default, SerializationConfig.PayloadHeaderSize);

            // receive the 4 bytes from docket 
            if (!_socket.ReceiveAsync(_sizeEventArgs)) OnMessageSizeReceiveCompleted(null, _sizeEventArgs);
        }

        private void OnMessageSizeReceiveCompleted(object _, SocketAsyncEventArgs args)
        {
            // get the transferred size
            var size = args.BytesTransferred;

            // if operation fails
            if (args.SocketError != SocketError.Success)
            {
                Close();
                return;
            }

            // check if size of header is invalid, close the connection
            if (size != SerializationConfig.PayloadHeaderSize)
            {
                Close();
                return;
            }

            // convert the first 4 bytes of the buffer to int32
            var msgSize = BitConverter.ToInt32(_receiveBuff.AsSpan(0, SerializationConfig.PayloadHeaderSize));

            // receive exactly the size converted from the last step
            ReceiveMsg(msgSize);
        }

        #endregion

        #region ReceiveMessage

        /// <summary>
        ///     this method will read exactly n bytes from socket
        /// </summary>
        /// <param name="msgSize">the size of message to read</param>
        private void ReceiveMsg(int msgSize)
        {
            if (msgSize <= 0)
            {
                Close();
                return;
            }

            if (!_connected)
                return;

            if (msgSize > _receiveBuff.Length - SerializationConfig.PayloadHeaderSize)
                SetupReceiveBufferWithSize(msgSize);

            _receiveEventArgs.SetBuffer(_receiveBuff, 0, msgSize);

            if (!_socket.ReceiveAsync(_receiveEventArgs))
                OnMessageReceiveCompleted(null, _receiveEventArgs);
        }

        private void OnMessageReceiveCompleted(object _, SocketAsyncEventArgs args)
        {
            // get the transferred size
            var size = args.BytesTransferred;

            // if operation fails
            if (args.SocketError != SocketError.Success)
            {
                Close();
                return;
            }

            // check if size is invalid, close the connection
            if (size <= 0)
            {
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
        ///     sets an action for when send async finished sending data
        /// </summary>
        /// <param name="onSendCompleted"></param>
        public void SetupSendCompletedHandler(Action<Guid> onSendCompleted, Action<Guid> onSendFailed)
        {
            _onSendCompletedHandler = onSendCompleted;
            _onSendFailedHandler = onSendFailed;
        }

        public void SetSendPayloadId(Guid sendPayloadId)
        {
            _sendPayloadId = sendPayloadId;
        }

        /// <summary>
        ///     Send is synchronous and will block until the message is sent
        ///     this method is only used for testing
        /// </summary>
        /// <param name="payload"></param>
        public void Send(Memory<byte> payload)
        {
            _socket.Send(payload.Span);
        }

        /// <summary>
        ///     SendAsync will asynchronously send the message, then it will call the complete handler
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public bool SendAsync(Memory<byte> payload)
        {
            if (_sendBuff.Length < payload.Length)
                SetupSendBufferWithSize(payload.Length);

            payload.CopyTo(_sendBuff);

            _sendEventArgs.SetBuffer(_sendBuff.AsMemory(0, payload.Length));

            return _socket.SendAsync(_sendEventArgs);
        }

        private void OnSendCompleted(object _, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                _onSendCompletedHandler?.Invoke(_sendPayloadId);
            }
            else
            {
                _onSendFailedHandler?.Invoke(_sendPayloadId);
            }
        }

        #endregion
    }
}