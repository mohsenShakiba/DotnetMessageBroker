using System;
using System.Buffers;
using System.Threading.Tasks;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using MessageBroker.TCP.SocketWrapper;

namespace MessageBroker.TCP.Client
{
    public class ClientSession : IClientSession
    {
        private readonly IBinaryDataProcessor _binaryDataProcessor;
        private bool _connected;
        private ITcpSocket _socket;
        private ISocketDataProcessor _socketDataProcessor;
        private ISocketEventProcessor _socketEventProcessor;
        private object _lock;
        public bool Debug { get; set; }

        public ClientSession(IBinaryDataProcessor binaryDataProcessor)
        {
            _binaryDataProcessor = binaryDataProcessor;
            Id = Guid.NewGuid();
            _lock = new object();
        }

        public Guid Id { get; }

        public void Use(ITcpSocket socket)
        {
            if (_connected)
                return;

            Logger.LogInformation($"SendQueue -> client session -> start receive process for {Id}");

            _socket = socket;
            _connected = true;

            if (_socketEventProcessor is null)
                throw new Exception("The socket event processor isn't provided");

            if (_socketDataProcessor is null)
                throw new Exception("The socket data processor isn't provided");

            StartReceiveProcess();
        }

        public void ForwardEventsTo(ISocketEventProcessor socketEventProcessor)
        {
            _socketEventProcessor = socketEventProcessor;
        }

        public void ForwardDataTo(ISocketDataProcessor socketDataProcessor)
        {
            _socketDataProcessor = socketDataProcessor;
            _binaryDataProcessor.Debug = Debug;
        }

        #region Close

        public void Close()
        {
            lock (_lock)
            {
                if (!_connected)
                    return;
                
                Logger.LogInformation($"SendQueue -> stopping client session {Id}");

                _connected = false;
            
                try
                {
                    if (_connected)
                        return;
                
                    _socket.Close();
            
                    _socketEventProcessor.ClientDisconnected(this);
                    Logger.LogInformation($"SendQueue -> stopping event notified {Id}");
                }
                catch (Exception e)
                {
                    Logger.LogInformation($"SendQueue -> stopping event cause exception {e}");
                }
            }
        }

        #endregion

        #region Receive

        /// <summary>
        ///     this method will start receiving message from socket
        /// </summary>
        private void StartReceiveProcess()
        {
            Task.Factory.StartNew(async () =>
            {
                while (_connected)
                {
                    await ReceiveAsync();
                    ProcessReceivedData();
                }
            }, TaskCreationOptions.LongRunning);
        }

        private async Task ReceiveAsync()
        {
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(BinaryProtocolConfiguration.ReceiveDataSize);

            var receivedSize = await _socket.ReceiveAsync(receiveBuffer);

            if (receivedSize == 0)
            {
                Close();
                return;
            }

            _binaryDataProcessor.Write(receiveBuffer.AsMemory(0, receivedSize));

            ArrayPool<byte>.Shared.Return(receiveBuffer);
        }

        private void ProcessReceivedData()
        {
            while (true)
            {
                var canRead = _binaryDataProcessor.TryRead(out var binaryPayload);

                if (canRead)
                {
                    _socketDataProcessor.DataReceived(Id, binaryPayload.DataWithoutSize);

                    binaryPayload.Dispose();

                    ObjectPool.Shared.Return(binaryPayload);
                }
                else
                {
                    break;
                }
            }
        }

        #endregion

        #region Send

        public async Task<bool> SendAsync(Memory<byte> payload)
        {
            if (!_connected)
                return false;

            var sendSize = await _socket.SendAsync(payload);

            if (sendSize < payload.Length)
            {
                Close();
                return false;
            }

            return true;
        }

        #endregion
    }
}