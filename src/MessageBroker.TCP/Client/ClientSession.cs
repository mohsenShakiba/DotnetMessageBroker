﻿using System;
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
        public bool Debug { get; set; }

        public ClientSession(IBinaryDataProcessor binaryDataProcessor)
        {
            _binaryDataProcessor = binaryDataProcessor;

            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public void Use(ITcpSocket socket)
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
            _binaryDataProcessor.Debug = Debug;
        }

        #region Close

        public void Close()
        {
            Logger.LogInformation($"stopping client session {Id}");

            _connected = false;

            _socket.Close();

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

            var receivedSize = await _socket.ReceiveAsync(receiveBuffer);

            if (receivedSize == 0)
            {
                Close();
                return;
            }

            _binaryDataProcessor.Write(receiveBuffer.AsMemory(0, receivedSize));
            
            Logger.LogInformation($"write from client session is {receivedSize}");

            ArrayPool<byte>.Shared.Return(receiveBuffer);
        }

        private void ProcessReceivedData()
        {
            Logger.LogInformation($"reading from binary data processor");
            while (true)
            {
                var canRead = _binaryDataProcessor.TryRead(out var binaryPayload);
                
                Logger.LogInformation($"Can read from client session is {canRead}");
                
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

        public bool Send(Memory<byte> payload)
        {
            return SendAsync(payload).Result;
        }

        #endregion
    }
}