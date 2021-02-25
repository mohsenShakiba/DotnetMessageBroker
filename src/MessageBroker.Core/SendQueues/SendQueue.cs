using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.Queues;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;
using MessageBroker.Serialization;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    /// <summary>
    ///     SendQueue is in charge of sending messages to subscribers
    ///     it will handle how many messages has been sent based on the concurrency requested
    /// </summary>
    public class SendQueue: ISendQueue
    {
        private readonly ConcurrentDictionary<Guid, SerializedPayload> _pendingMessages;
        private readonly Channel<SerializedPayload> _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        private SemaphoreSlim _sendSemaphore;
        private bool _autoAck;
        private int _currentConcurrencyLevel;

        public int Available => _sendSemaphore.CurrentCount;
        public IClientSession Session { get; }

        public SendQueue(IClientSession session)
        {
            Session = session;
            _cancellationTokenSource = new CancellationTokenSource();
            _queue = Channel.CreateUnbounded<SerializedPayload>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
            _semaphore = new SemaphoreSlim(1, 1);
            _pendingMessages = new ConcurrentDictionary<Guid, SerializedPayload>();

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
            // cancel all pending process
            _cancellationTokenSource.Cancel();
            
            // dispose serialized payloads
            while (_queue.Reader.TryRead(out var serializedPayload))
            {
                serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                ObjectPool.Shared.Return(serializedPayload);
            }

            // foreach pending message, requeue
            foreach (var (_, serializedPayload) in _pendingMessages)
            {
                serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                ObjectPool.Shared.Return(serializedPayload);
            }
        }

        public void Enqueue(SerializedPayload serializedPayload)
        {
            _queue.Writer.TryWrite(serializedPayload);
        }

        public void OnMessageAckReceived(Guid messageId)
        {
            if (_pendingMessages.TryRemove(messageId, out var serializedPayload))
            {
                serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Ack);
                ObjectPool.Shared.Return(serializedPayload);
                _sendSemaphore.Release();
            }
        }

        public void OnMessageNackReceived(Guid messageId)
        {
            if (_pendingMessages.Remove(messageId, out var serializedPayload))
            {
                serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                ObjectPool.Shared.Return(serializedPayload);
                _sendSemaphore.Release();
            }
        }

        private async Task SendMessagePayloadAsync(SerializedPayload serializedPayload)
        {
            try
            {
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                await _sendSemaphore.WaitAsync(_cancellationTokenSource.Token);

                // add message to pending message dict
                _pendingMessages[serializedPayload.Id] = serializedPayload;
                
                // send the payload 
                var success = await Session.SendAsync(serializedPayload.Data);

                // notify message ack it is auto ack
                if (success && _autoAck)
                    OnMessageAckReceived(serializedPayload.Id);
                // notify message nack if it was not sent
                else if (!success)
                    OnMessageNackReceived(serializedPayload.Id);
            }
            catch
            {
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
     
                ObjectPool.Shared.Return(serializedPayload);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}