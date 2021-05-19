using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Async;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using MessageBroker.Common.Tcp;
using MessageBroker.Common.Tcp.EventArgs;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Core.Clients
{
    /// <inheritdoc />
    public class Client : IClient
    {
        /// <summary>
        /// The <see cref="IBinaryDataProcessor" /> used for storing batch data received from connection, processing them and
        /// retrieving them
        /// </summary>
        private readonly IBinaryDataProcessor _binaryDataProcessor;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<Client> _logger;
        private readonly Channel<SerializedPayload> _queue;

        /// <summary>
        /// A buffer used to store temporary received data
        /// </summary>
        private readonly byte[] _receiveBuffer;

        private readonly ConcurrentDictionary<Guid, AsyncPayloadTicket> _tickets;

        /// <summary>
        /// The underlying <see cref="ISocket" /> used for sending an receiving data
        /// </summary>
        private ISocket _socket;


        /// <summary>
        /// Will create a new <see cref="Client" />
        /// </summary>
        /// <param name="logger">Logger used for client</param>
        /// <param name="binaryDataProcessor">Optional <see cref="IBinaryDataProcessor" /> used for processing data</param>
        public Client(ILogger<Client> logger, IBinaryDataProcessor binaryDataProcessor = null)
        {
            _logger = logger;

            // set a random id
            Id = Guid.NewGuid();

            // set binary data processor and set default if null
            _binaryDataProcessor = binaryDataProcessor ?? new BinaryDataProcessor();

            // rent buffer using for receiving data
            _receiveBuffer = ArrayPool<byte>.Shared.Rent(BinaryProtocolConfiguration.ReceiveDataSize);

            // set cancellation token
            _cancellationTokenSource = new CancellationTokenSource();

            // create channel for send queue
            _queue = Channel.CreateUnbounded<SerializedPayload>();

            // dictionary for storing tickets
            _tickets = new ConcurrentDictionary<Guid, AsyncPayloadTicket>();

            // set max concurrency
            // default max concurrency is 100
            MaxConcurrency = 100;
        }

        /// <inheritdoc />
        public Guid Id { get; }

        /// <inheritdoc />
        public int MaxConcurrency { get; private set; }

        /// <inheritdoc />
        public bool ReachedMaxConcurrency => _tickets.Count >= MaxConcurrency;

        /// <inheritdoc />
        public bool IsClosed { get; private set; }

        /// <inheritdoc />
        public event EventHandler<ClientSessionDisconnectedEventArgs> OnDisconnected;

        /// <inheritdoc />
        public event EventHandler<ClientSessionDataReceivedEventArgs> OnDataReceived;

        /// <inheritdoc />
        public void Setup(ISocket socket)
        {
            if (!socket.Connected)
                throw new InvalidOperationException("The provided tcp socket was not in connected state");

            _socket = socket;
        }

        /// <inheritdoc />
        public void Close()
        {
            // we need to lock the close method
            // otherwise multiple concurrent calls to Close will cause the OnDisconnected to be called twice
            lock (this)
            {
                if (IsClosed) return;

                try
                {
                    _logger.LogInformation($"Dispose was called on client: {Id}");

                    // mark as disposed
                    IsClosed = true;

                    // complete the channel 
                    _queue.Writer.TryComplete();

                    // cancelling the receive cancellation token
                    _cancellationTokenSource.Cancel();

                    // setting status for all the tickets
                    SetStatusForAllPendingTickets();

                    // disconnect the socket
                    if (_socket.Connected) _socket.Disconnect();

                    // we need to invoice OnDisconnected on a separate thread
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var disconnectedEventArgs = new ClientSessionDisconnectedEventArgs {Id = Id};
                        OnDisconnected?.Invoke(this, disconnectedEventArgs);
                        OnDisconnected = null;
                    });
                }
                catch
                {
                    // no-op
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Close();
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

            ArrayPool<byte>.Shared.Return(_receiveBuffer);

            OnDataReceived = null;

            // this line is moved to close method after invoking OnDisconnected
            // OnDisconnected = null;
        }

        private void SetStatusForAllPendingTickets()
        {
            // set status of all the tickets
            foreach (var (_, ticket) in _tickets)
                try
                {
                    _logger.LogTrace($"Ticket for message: {ticket.PayloadId} was disposed");

                    ticket.SetStatus(false);
                    ObjectPool.Shared.Return(ticket);
                }
                catch
                {
                    // no-op, since the status might have been set caused by race condition
                }

            _tickets.Clear();

            // dispose all items in queue
            while (_queue.Reader.TryRead(out var serializedPayload)) ObjectPool.Shared.Return(serializedPayload);
        }

        private void ThrowIfDisposed()
        {
            if (IsClosed)
                throw new ObjectDisposedException("Session has been disposed previously");
        }

        #region Receive

        /// <inheritdoc />
        public void StartReceiveProcess()
        {
            ThrowIfDisposed();

            Task.Factory.StartNew(async () =>
            {
                while (!IsClosed) await ReceiveAsync();

                OnReceivedDataDisposed();
            }, TaskCreationOptions.LongRunning);
        }


        /// <summary>
        /// Will receive data from socket and write to BinaryDataProcessor
        /// </summary>
        private async ValueTask ReceiveAsync()
        {
            var receivedSize = await _socket.ReceiveAsync(_receiveBuffer, _cancellationTokenSource.Token);

            if (receivedSize == 0)
            {
                Close();
                return;
            }

            _binaryDataProcessor.Write(_receiveBuffer.AsMemory(0, receivedSize));

            ProcessReceivedData();
        }

        /// <summary>
        /// Will try to read the payload from BinaryDataProcessor
        /// if any payload is completely received then it is dispatched to OnDataReceived event
        /// </summary>
        private void ProcessReceivedData()
        {
            try
            {
                // we are calling BeginLock so that if Dispose is called on BinaryDataProcessor it waits until EndLock is called
                _binaryDataProcessor.BeginLock();

                while (_binaryDataProcessor.TryRead(out var binaryPayload))
                    try
                    {
                        var dataReceivedEventArgs = new ClientSessionDataReceivedEventArgs
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
            finally
            {
                _binaryDataProcessor.EndLock();
            }
        }

        #endregion

        #region Send

        /// <inheritdoc />
        public void StartSendProcess()
        {
            ThrowIfDisposed();

            Task.Factory.StartNew(async () =>
            {
                while (!IsClosed) await SendNextMessageInQueue();
            });
        }

        /// <inheritdoc />
        public async Task SendNextMessageInQueue()
        {
            var serializedPayload = await _queue.Reader.ReadAsync();

            var result = await SendAsync(serializedPayload.Data, CancellationToken.None);

            _logger.LogTrace($"Sending message: {serializedPayload.PayloadId} to client: {Id}");

            if (!result) DisposeMessagePayloadAndSetStatus(serializedPayload.PayloadId, false);

            ObjectPool.Shared.Return(serializedPayload);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void EnqueueFireAndForget(SerializedPayload serializedPayload)
        {
            _queue.Writer.TryWrite(serializedPayload);
        }

        /// <inheritdoc />
        public void OnPayloadAckReceived(Guid payloadId)
        {
            DisposeMessagePayloadAndSetStatus(payloadId, true);
        }

        /// <inheritdoc />
        public void OnPayloadNackReceived(Guid payloadId)
        {
            DisposeMessagePayloadAndSetStatus(payloadId, false);
        }

        /// <inheritdoc />
        public void ConfigureConcurrency(int maxConcurrency)
        {
            MaxConcurrency = maxConcurrency;
        }

        /// <inheritdoc />
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
    }
}