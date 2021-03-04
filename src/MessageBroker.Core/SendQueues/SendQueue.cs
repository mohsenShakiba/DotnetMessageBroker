using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    /// <summary>
    ///     SendQueue is in charge of sending messages to subscribers
    ///     it will handle how many messages has been sent based on the concurrency requested
    /// </summary>
    public class SendQueue : ISendQueue
    {
        private readonly ConcurrentDictionary<Guid, SerializedPayload> _pendingMessages;
        private readonly Channel<SerializedPayload> _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private SemaphoreSlim _sendSemaphore;
        private bool _autoAck;
        private int _currentConcurrencyLevel;
        private bool _stopped;
        private object _lock;

        public int Available => _sendSemaphore.CurrentCount;
        public IClientSession Session { get; }

        public SendQueue(IClientSession session)
        {
            Session = session;
            _cancellationTokenSource = new();
            _lock = new();
            _pendingMessages = new();
            _queue = Channel.CreateUnbounded<SerializedPayload>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
            _semaphore = new SemaphoreSlim(1, 1);

            _currentConcurrencyLevel = 10;
            _sendSemaphore = new SemaphoreSlim(_currentConcurrencyLevel, _currentConcurrencyLevel);

            SetupQueueRunner();
        }

        private void SetupQueueRunner()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    try
                    {
                        // read the serialized payload from reader
                        var serializedPayload = await _queue.Reader.ReadAsync();

                        if (serializedPayload.Type == PayloadType.Msg)
                            await SendMessagePayloadAsync(serializedPayload);
                        else
                            await SendNonMessagePayloadAsync(serializedPayload);
                    }
                    catch
                    {
                        // if there is an exception, just ignore
                    }
            }, TaskCreationOptions.LongRunning);
        }

        public void Configure(int concurrency, bool autoAck)
        {
            _autoAck = autoAck;
            _sendSemaphore = new SemaphoreSlim(concurrency - (_currentConcurrencyLevel - _sendSemaphore.CurrentCount),
                concurrency);
            _currentConcurrencyLevel = concurrency;
        }

        /// <summary>
        ///     Stop will send fail message event for all messages in queue and in pending messages list
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _stopped = true;
                
                Logger.LogInformation($"SendQueue -> Begin stopping {Session.Id}");
                // cancel all pending process
                _cancellationTokenSource.Cancel();

                // foreach pending message, requeue
                foreach (var (_, serializedPayload) in _pendingMessages)
                {
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    Logger.LogInformation($"SendQueue -> payload: {serializedPayload.Id} was nacked from send queue stop");
                    ObjectPool.Shared.Return(serializedPayload);
                }
                
                // dispose serialized payloads
                while (_queue.Reader.TryRead(out var serializedPayload))
                {
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    Logger.LogInformation($"SendQueue -> payload: {serializedPayload.Id} was nacked from send queue stop");
                    ObjectPool.Shared.Return(serializedPayload);
                }
                Logger.LogInformation($"SendQueue -> End stopping {Session.Id}");
                
                _pendingMessages.Clear();
            }
        }

        public void Enqueue(SerializedPayload serializedPayload)
        {
            lock (_lock)
            {
                if (_stopped)
                {
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    ObjectPool.Shared.Return(serializedPayload);
                    return;
                }
                Logger.LogInformation($"SendQueue -> Received payload: {serializedPayload.Id} {Session.Id} {_stopped}");
                _queue.Writer.TryWrite(serializedPayload);
            }
        }

        public void OnMessageAckReceived(Guid messageId)
        {
            lock (_lock)
            {
                if (_pendingMessages.TryRemove(messageId, out var serializedPayload))
                {
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Ack);
                    Logger.LogInformation($"SendQueue -> acked payload: {serializedPayload.Id}");
                    ObjectPool.Shared.Return(serializedPayload);
                    _sendSemaphore.Release();
                }
                else
                {
                    Logger.LogInformation($"SendQueue -> ack not released {messageId}");
                }
            }
        }

        public void OnMessageNackReceived(Guid messageId)
        {
            lock (_lock)
            {
                if (_pendingMessages.Remove(messageId, out var serializedPayload))
                {
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    Logger.LogInformation($"SendQueue -> nacked payload: {serializedPayload.Id}");
                    ObjectPool.Shared.Return(serializedPayload);
                    _sendSemaphore.Release();
                }
                else
                {
                    Logger.LogInformation($"SendQueue -> nack not released {messageId}");
                }
            }
        }

        private async Task SendMessagePayloadAsync(SerializedPayload serializedPayload)
        {
            try
            {
                Logger.LogInformation($"SendQueue -> preparing payload: {serializedPayload.Id} {_sendSemaphore.CurrentCount}");
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                Logger.LogInformation($"SendQueue -> preparing payload 1: {serializedPayload.Id} {_sendSemaphore.CurrentCount}");
                await _sendSemaphore.WaitAsync(_cancellationTokenSource.Token);
                Logger.LogInformation($"SendQueue -> preparing payload 2: {serializedPayload.Id} {_sendSemaphore.CurrentCount}");
                
                lock (_lock)
                {
                    // add message to pending message dict
                    _pendingMessages[serializedPayload.Id] = serializedPayload;
                }

                // send the payload 
                var success = await Session.SendAsync(serializedPayload.Data);

                if (success)
                {
                    Logger.LogInformation($"SendQueue -> payload: {serializedPayload.Id} was sent");
                }
                else
                {
                    Logger.LogInformation($"SendQueue -> payload: {serializedPayload.Id} was not sent");
                }
                
                // notify message ack it is auto ack
                if (success && _autoAck)
                    OnMessageAckReceived(serializedPayload.Id);
                // notify message nack if it was not sent
                else if (!success)
                    OnMessageNackReceived(serializedPayload.Id);
            }
            catch
            {
                Logger.LogInformation($"SendQueue -> nacked payload: {serializedPayload.Id} from catch");
                serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                ObjectPool.Shared.Return(serializedPayload);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SendNonMessagePayloadAsync(SerializedPayload serializedPayload)
        {
            try
            {
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);

                await Session.SendAsync(serializedPayload.Data);
                
                Logger.LogInformation($"SendQueue -> payload: {serializedPayload.Id} disposed");


                ObjectPool.Shared.Return(serializedPayload);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}