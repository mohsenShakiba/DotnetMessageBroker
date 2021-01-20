using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Buffers;
using MessageBroker.Client.EventStores;
using MessageBroker.Client.TaskManager;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client.SocketClient
{
    internal class SocketClient : ISocketClient
    {
        private readonly IEventStore _eventStore;
        private readonly ILogger<SocketClient> _logger;
        private readonly ManualResetEventSlim _sendResetEvent;
        private readonly Socket _socket;
        private readonly ITaskManager _taskManager;
        private MemoryBuffer _buffer;
        private bool _connected;
        private bool _connecting;
        private IPEndPoint _endPoint;
        private SocketAsyncEventArgs _sendEventArgs;
        private SocketAsyncEventArgs _receiveSizeEventArgs;
        private SocketAsyncEventArgs _receiveEventArgs;
        private byte[] _receiveBuff;
        private TaskCompletionSource<Memory<byte>> _receiveTask;

        public SocketClient(ILogger<SocketClient> logger, IEventStore eventStore, ITaskManager taskManager)
        {
            _logger = logger;
            _eventStore = eventStore;
            _taskManager = taskManager;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _buffer = new MemoryBuffer();
            _sendResetEvent = new ManualResetEventSlim();

            _receiveBuff = ArrayPool<byte>.Shared.Rent(1024);
            _sendEventArgs.Completed += OnSendAsyncCompleted;
            _receiveSizeEventArgs.Completed += OnReceiveSizeCompleted;
            _receiveEventArgs.Completed += OnReceiveCompleted;
        }

        public void Connect(IPEndPoint endPoint, bool retryOnFailure)
        {
            if (_connecting)
                return;

            _endPoint = endPoint;
            _connecting = true;

            try
            {
                _socket.Connect(_endPoint);
                _logger.LogInformation("Client successfully connected to server");
                _connected = true;
                _connecting = false;
            }
            catch (SocketException)
            {
                if (!retryOnFailure)
                    throw;

                _logger.LogWarning($"Failed to connect to server with endpoint: {_endPoint}");
                _connecting = false;
                Reconnect();
            }
        }

        public Task<bool> SendAsync(Guid payloadId, Memory<byte> payload, bool completeOnAcknowledge)
        {
            _sendResetEvent.Wait();

            var task = _taskManager.Setup(payloadId, completeOnAcknowledge);

            _sendEventArgs.SetBuffer(payload);
            _socket.SendAsync(payload, SocketFlags.None);

            _sendEventArgs.UserToken = payloadId;

            return task;
        }

        public Task<Memory<byte>> ReceiveAsync()
        {
            var tcs = new TaskCompletionSource<Memory<byte>>();

            _receiveTask = tcs;
            
            _receiveSizeEventArgs.SetBuffer(_receiveBuff, 0, 4);

            if (!_socket.ReceiveAsync(_receiveSizeEventArgs)) OnReceiveCompleted(null, _receiveSizeEventArgs);

            return tcs.Task;
        }

        private void Reconnect()
        {
            if (_connected || _connecting)
            {
                _logger.LogWarning("Retry in not possible when client is connected or trying to connect");
                return;
            }

            _connecting = true;

            while (true)
                try
                {
                    _socket.Connect(_endPoint);
                    _connected = true;
                    _connecting = false;
                    _logger.LogInformation("Client successfully connected to server");
                    break;
                }
                catch (SocketException)
                {
                    _logger.LogWarning($"Failed to connect to server with endpoint: {_endPoint}");
                }
        }

        private void OnSendAsyncCompleted(object _, SocketAsyncEventArgs args)
        {
            _sendResetEvent.Set();

            var payloadId = (Guid?) args.UserToken;

            if (payloadId == null)
                throw new InvalidOperationException();

            _eventStore.OnSent(payloadId.Value);

            if (args.SocketError != SocketError.Success)
            {
                _logger.LogWarning("Failed to send data, looks like the connection is broker");
                _logger.LogWarning("Trying to reconnect");
                Reconnect();
            }
        }

        private void OnReceiveSizeCompleted(object _, SocketAsyncEventArgs args)
        {
            var bytesTransferred = args.BytesTransferred;

            if (bytesTransferred < 4)
                throw new InvalidOperationException();

            if (args.SocketError != SocketError.Success)
                throw new InvalidOperationException();

            var messageSize = BitConverter.ToInt32(_receiveBuff);

            ReceiveAsync(messageSize);
        }

        private void ReceiveAsync(int msgSize)
        {
            _receiveEventArgs.SetBuffer(_receiveBuff, 0, msgSize);
            
            if (!_socket.ReceiveAsync(_receiveEventArgs)) OnReceiveCompleted(null, _receiveEventArgs);
        }

        private void OnReceiveCompleted(object _, SocketAsyncEventArgs args)
        {
            var bytesTransferred = args.BytesTransferred;
            
            if (bytesTransferred <= 0)
                throw new InvalidOperationException();
            
            if (args.SocketError != SocketError.Success)
                throw new InvalidOperationException();

            _receiveTask.TrySetResult(_receiveBuff.AsMemory(0, bytesTransferred));
        }
    }
}