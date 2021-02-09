using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Core.Socket.Client
{
    public class ClientSession : IClientSession
    {
        private readonly System.Net.Sockets.Socket _socket;
        private readonly ISocketEventProcessor _eventProcessor;
        private readonly IBinaryDataProcessor _binaryDataProcessor;
        
        private bool _connected;
        
        public Guid Id { get; }

        public ClientSession(ISocketEventProcessor eventProcessor, System.Net.Sockets.Socket socket)
        {
            _binaryDataProcessor = new BinaryDataProcessor();

            _eventProcessor = eventProcessor;
            _socket = socket;

            _connected = true;
            Id = Guid.NewGuid();

            StartReceiveProcess();
        }
        
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
            {
                if (_binaryDataProcessor.TryRead(out var binaryPayload))
                {
                    _eventProcessor.DataReceived(Id, binaryPayload.DataWithoutSize);

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

            var sendSize = await _socket.SendAsync(payload, SocketFlags.None);

            if (sendSize < payload.Length)
                Close();

            return true;
        }

        public bool Send(Memory<byte> payload)
        {
            return SendAsync(payload).Result;
        }

        #endregion
        
        #region Close

        public void Close()
        {
            Logger.LogInformation($"stopping client session {Id}");

            _connected = false;

            _socket.Close();
            _socket.Dispose();

            _eventProcessor.ClientDisconnected(this);
        }


        #endregion

    }
}