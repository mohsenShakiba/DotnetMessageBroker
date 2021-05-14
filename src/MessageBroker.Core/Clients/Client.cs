using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Models.Async;
using MessageBroker.Serialization;
using MessageBroker.TCP;
using MessageBroker.TCP.Binary;
using MessageBroker.TCP.EventArgs;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Core.Clients
{
    /// <inheritdoc />
    public class Client : IClient
    {
        /// <summary>
        /// The <see cref="IBinaryDataProcessor"/> used for storing batch data received from connection, processing them and retrieving them
        /// </summary>
        private readonly IBinaryDataProcessor _binaryDataProcessor;

        /// <summary>
        /// The underlying <see cref="ITcpSocket"/> used for sending an receiving data
        /// </summary>
        private readonly ITcpSocket _socket;

        /// <summary>
        /// A buffer used to store temporary received data
        /// </summary>
        private readonly byte[] _receiveBuffer;

        private readonly ILogger<Client> _logger;
        private readonly ConcurrentDictionary<Guid, AsyncPayloadTicket> _tickets;
        private readonly Channel<SerializedPayload> _queue;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Guid Id { get; }
        public bool Debug { get; set; }
        public bool ReachedMaxConcurrency => false;

        public event EventHandler<ClientSessionDisconnectedEventArgs> OnDisconnected;

        public event EventHandler<ClientSessionDataReceivedEventArgs> OnDataReceived;

        private bool _disposed;
        private int _count;

        public Client(ITcpSocket tcpSocket, ILogger<Client> logger, IBinaryDataProcessor binaryDataProcessor = null)
        {
            if (!tcpSocket.Connected)
                throw new InvalidOperationException("The provided tcp socket was not in connected state");

            // set a random id
            Id = Guid.NewGuid();

            _socket = tcpSocket;
            _logger = logger;

            // set binary data processor and set default if null
            _binaryDataProcessor = binaryDataProcessor ?? new BinaryDataProcessor();

            // rent buffer using for receiving data
            _receiveBuffer = ArrayPool<byte>.Shared.Rent(BinaryProtocolConfiguration.ReceiveDataSize);

            // set cancellation token
            _cancellationTokenSource = new CancellationTokenSource();

            // create channel for send queue
            _queue = Channel.CreateUnbounded<SerializedPayload>();

            // dictionary for storing tickets
            _tickets = new();
        }

        public void StartReceiveProcess()
        {
            ThrowIfDisposed();

            Task.Factory.StartNew(async () =>
            {
                while (!_disposed)
                {
                    await ReceiveAsync();
                }

                OnReceivedDataDisposed();
            }, TaskCreationOptions.LongRunning);
        }

        public void StartSendProcess()
        {
            ThrowIfDisposed();

            Task.Factory.StartNew(async () =>
            {
                while (!_disposed)
                {
                    var serializedPayload = await _queue.Reader.ReadAsync();

                    var result = await SendAsync(serializedPayload.Data, CancellationToken.None);

                    _logger.LogTrace($"Sending message: {serializedPayload.PayloadId} to client: {Id}");

                    if (!result)
                    {
                        DisposeMessagePayloadAndSetStatus(serializedPayload.PayloadId, false);
                    }

                    ObjectPool.Shared.Return(serializedPayload);
                }
            });
        }

        #region Receive

        /// <summary>
        /// Will receive data from socket and write to BinaryDataProcessor
        /// </summary>
        private async ValueTask ReceiveAsync()
        {

            try
            {
                var receivedSize = await _socket.ReceiveAsync(_receiveBuffer, _cancellationTokenSource.Token);

                if (receivedSize == 0)
                {
                    Close();
                    return;
                }

                var data = buff.AsMemory(0, receivedSize).ToArray();
                _binaryDataProcessor.Write(data);

                ProcessReceivedData();
            }
            catch
            {
                Logger.LogInformation("exception");
            }
            finally
            {
                Interlocked.Decrement(ref _count);
            }
        }

        /// <summary>
        /// Will try to read the payload from BinaryDataProcessor
        /// if any payload is completely received then it is dispatched to OnDataReceived event
        /// </summary>
        private void ProcessReceivedData()
        {
            try
            {
                _binaryDataProcessor.BeginLock();
                
                while (_binaryDataProcessor.TryRead(out var binaryPayload))
                {
                    try
                    {
                        var deserialized = new Deserializer();

                        var type = deserialized.ParsePayloadType(binaryPayload.DataWithoutSize);

                        if (type == PayloadType.Msg)
                        {
                            var msg = deserialized.ToMessage(binaryPayload.DataWithoutSize);
                            
                            Logger.LogInformation($"binary, received data with id {msg.Id}");
                        }
                        
                        var dataReceivedEventArgs = new ClientSessionDataReceivedEventArgs()
                        {
                            Data = binaryPayload.DataWithoutSize,
                            Id = Id
                        };

                        OnDataReceived?.Invoke(this, dataReceivedEventArgs);
                    }
                    finally
                    {
                        binaryPayload.Dispose();
                    }
                }
            }
            finally
            {
                _binaryDataProcessor.EndLock();
            }
        }

        #endregion

        #region Send

        public AsyncPayloadTicket Enqueue(SerializedPayload serializedPayload)
        {
            lock (this)
            {
                var queueWasSuccessful = _queue.Writer.TryWrite(serializedPayload);

                if (queueWasSuccessful)
                {
                    _logger.LogTrace($"Enqueue message: {serializedPayload.PayloadId} in client: {Id}");

                    var ticket = ObjectPool.Shared.Rent<AsyncPayloadTicket>();

                    ticket.Setup(serializedPayload.PayloadId);

                    _tickets[ticket.PayloadId] = ticket;

                    return ticket;
                }

                throw new ChannelClosedException();
            }
        }

        public void EnqueueFireAndForget(SerializedPayload serializedPayload)
        {
            _queue.Writer.TryWrite(serializedPayload);
        }

        public void OnPayloadAckReceived(Guid payloadId)
        {
            DisposeMessagePayloadAndSetStatus(payloadId, true);
        }

        public void OnPayloadNackReceived(Guid payloadId)
        {
            DisposeMessagePayloadAndSetStatus(payloadId, false);
        }

        public async Task<bool> SendAsync(Memory<byte> payload, CancellationToken cancellationToken)
        {
            var sendSize = await _socket.SendAsync(payload, cancellationToken);

            if (sendSize == 0)
            {
                Close();
                return false;
            }

            return true;
        }

        #endregion

        public void Close()
        {
            // we need to lock the close method
            // otherwise multiple concurrent calls to Close will cause the OnDisconnected to be called twice
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }

                try
                {

                    _logger.LogInformation($"Dispose was called on client: {Id}");

                    _disposed = true;

                    // complete the channel 
                    _queue.Writer.TryComplete();

                    _cancellationTokenSource.Cancel();

                    SetStatusForAllPendingTickets();

                    if (_socket.Connected)
                    {
                        _socket.Disconnect();
                    }

                    // we need to invoice OnDisconnected on a separate thread
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var disconnectedEventArgs = new ClientSessionDisconnectedEventArgs { Id = Id };
                        OnDisconnected?.Invoke(this, disconnectedEventArgs);
                    });

                    Dispose();
                }
                catch
                {
                    // no-op
                }

            }
        }

        public void Dispose()
        {


            _socket.Dispose();

            // todo:
            // ArrayPool<byte>.Shared.Return(_receiveBuffer, true);
        }

        private void DisposeMessagePayloadAndSetStatus(Guid payloadId, bool ack)
        {
            try
            {
                if (_tickets.Remove(payloadId, out var ticket))
                {
                    var type = ack ? "Ack" : "nack";

                    _logger.LogTrace($"{type} received for message: {payloadId}");

                    ticket.SetStatus(ack);

                    ObjectPool.Shared.Return(ticket);
                }
            }
            catch
            {
                // do nothing
            }
        }

        private void OnReceivedDataDisposed()
        {
            _binaryDataProcessor.Dispose();

            // OnDataReceived = null;
            // OnDisconnected = null;
        }

        private void SetStatusForAllPendingTickets()
        {
            Logger.LogInformation($"SetStatusForAllPendingTickets was called for {_tickets.Count} in {Id}");
            // set status of all the tickets
            foreach (var (_, ticket) in _tickets)
            {
                try
                {
                    Logger.LogInformation($"ticket for id {ticket.PayloadId} returned");
                    try
                    {
                        ticket.SetStatus(false);
                        ObjectPool.Shared.Return(ticket);
                    }
                    catch (Exception e)
                    {
                    }
                }
                catch
                {
                    // no-op, since the status might have been set caused by race condition
                }
            }

            _tickets.Clear();

            // dispose all items in queue
            while (_queue.Reader.TryRead(out var serializedPayload))
            {
                ObjectPool.Shared.Return(serializedPayload);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Session has been disposed previously");
        }
    }
}