using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Socket.Client
{
    public class ClientSession : IClientSession
    {
        private readonly IBinaryDataProcessor _binaryDataProcessor;
        private bool _connected;
        private System.Net.Sockets.Socket _socket;
        private ISocketDataProcessor _socketDataProcessor;

        private ISocketEventProcessor _socketEventProcessor;

        public ClientSession(IBinaryDataProcessor binaryDataProcessor)
        {
            _binaryDataProcessor = binaryDataProcessor;

            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public void Use(System.Net.Sockets.Socket socket)
        {
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
        }

        #region Close

        public void Close()
        {
            Logger.LogInformation($"stopping client session {Id}");

            _connected = false;

            _socket.Close();
            _socket.Dispose();

            _socketEventProcessor.ClientDisconnected(this);
        }

        #endregion

        #region Receive

        /// <summary>
        ///     this method will start recieving message from socket
        /// </summary>
        private void StartReceiveProcess()
        {
            Logger.LogInformation($"starting client session {Id}");

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

            var receivedSize = await _socket.ReceiveAsync(receiveBuffer, SocketFlags.None);

            if (receivedSize == 0)
            {
                Close();
                return;
            }

            _binaryDataProcessor.Write(receiveBuffer.AsMemory(0, receivedSize));
        }

        private void ProcessReceivedData()
        {
            while (true)
                if (_binaryDataProcessor.TryRead(out var binaryPayload))
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

        #endregion

        #region Send

        public async Task<bool> SendAsync(Memory<byte> payload)
        {
            if (!_connected)
                return false;

            var sendSize = await _socket.SendAsync(payload, SocketFlags.None);

            if (sendSize < payload.Length)
            {
                Close();
                return false;
            }

            return true;
        }

        public bool Send(Memory<byte> payload)
        {
            return SendAsync(payload).Result;
        }

        #endregion
    }
}