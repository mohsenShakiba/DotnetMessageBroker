using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManager;
using MessageBroker.Client.EventStores;
using MessageBroker.Client.Models;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client.SocketClient
{
    /// <summary>
    /// the purpose of this class is to enable send and receiving payloads
    /// </summary>
    internal class SocketClient : ISocketClient
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITaskManager _taskManager;
        private readonly IReceiveDataProcessor _receiveDataProcessor;
        private readonly Channel<SendData> _sendDataChannel;

        private readonly IConnectionManager _connectionManager;

        private SocketAsyncEventArgs _sendEventArgs;
        private SocketAsyncEventArgs _receiveEventArgs;

        private byte[] _receiveBuff;
        private bool _stopped;

        public ChannelWriter<SendData> SendDataChannel => _sendDataChannel.Writer;


        public SocketClient(ILoggerFactory loggerFactory, ITaskManager taskManager, IReceiveDataProcessor receiveDataProcessor,
            IConnectionManager connectionManager)
        {
            _loggerFactory = loggerFactory;
            _taskManager = taskManager;
            _receiveDataProcessor = receiveDataProcessor;
            _connectionManager = connectionManager;

            _sendDataChannel =
                Channel.CreateBounded<SendData>(ClientConfiguration.CurrentConfiguration.SendMessageChannelSize);

            SetupSendChannelProcessor();
            SetupReceiveChannelProcessor();
        }

        private void SetupSendChannelProcessor()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_stopped)
                {
                    var sendData = await _sendDataChannel.Reader.ReadAsync();
                    
                    await CheckSocketConnection();

                    await TrySendAsync(sendData);
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void SetupReceiveChannelProcessor()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_stopped)
                {
                    var receiveData = await ReceiveAsync();
                    
                    _receiveDataProcessor.AddReceiveDataChunk(receiveData);
                }
            }, TaskCreationOptions.LongRunning);
        }

        private async ValueTask CheckSocketConnection()
        {
            if (_connectionManager.IsConnected)
                return;

            while (!_connectionManager.IsConnected && !_stopped)
            {
                await Task.Delay(1000);
            }
        }

        public void Connect(SocketConnectionConfiguration configuration)
        {
            _connectionManager.Connect(configuration);
        }

        public Task<SendAsyncResult> SendAsync(Guid id, Memory<byte> data, bool completeOnAcknowledge)
        {
            var task = _taskManager.Setup(id, completeOnAcknowledge);

            var sendData = new SendData
            {
                Data = data,
                Id = id
            };

            _sendDataChannel.Writer.WriteAsync(sendData);
            
            return task;
        }

        private async Task TrySendAsync(SendData sendData)
        {
            var retryCount = 0;

            while (true)
            {
                var result = await SendToSocketAsync(sendData.Data);

                retryCount += 1;

                if (!result && retryCount < ClientConfiguration.CurrentConfiguration.MaxSandRetryCount)
                    continue;

                if (result)
                    _taskManager.OnPayloadEvent(sendData.Id, SendEventType.Sent, null);
                else
                {
                    var errMessage = _connectionManager.LastSocketError;
                    _taskManager.OnPayloadEvent(sendData.Id, SendEventType.Failed, errMessage);
                }

                break;                    
            }
        }

        private async Task<bool> SendToSocketAsync(Memory<byte> payload)
        {
            var sentCount = await _connectionManager.Socket.SendAsync(payload, SocketFlags.None);

            if (sentCount == payload.Length)
                return true;

            _connectionManager.CheckConnectionStatusAndRetryIfDisconnected();

            await Task.Delay(1000);

            return false;
        }

        private async Task<Memory<byte>> ReceiveAsync()
        {
            _receiveBuff = ArrayPool<byte>.Shared.Rent(ClientConfiguration.CurrentConfiguration.ReceiveDataBufferSize);

            var receiveSize = await _connectionManager.Socket.ReceiveAsync(_receiveBuff, SocketFlags.None);

            return _receiveBuff.AsMemory(0, receiveSize);
        }
        
    }
}