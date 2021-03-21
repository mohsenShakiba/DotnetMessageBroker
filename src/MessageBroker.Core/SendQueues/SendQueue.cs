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
        private readonly SendQueueAvailabilityTicket _availabilityTicket;
        private readonly Channel<SerializedPayload> _queue;
        private readonly IClientSession _session;
        private readonly object _lock;
        private readonly CancellationTokenSource _cts;
        private SemaphoreSlim _sendPrefetchThrottler;

        private bool _autoAck;
        private bool _stopped;
        private int _maxConcurrencyLevel;

        public Guid Id => _session.Id;
        public bool IsAvailable => AvailableCount > 0;
        public int AvailableCount => _maxConcurrencyLevel - _pendingMessages.Count;


        public SendQueueAvailabilityTicket AvailabilityTicket
        {
            get
            {
                _availabilityTicket.SetAvailability(AvailableCount);
                return _availabilityTicket;
            }
        }

        public event Action<SendQueueAvailabilityTicket> OnAvailable;

        public SendQueue(IClientSession session)
        {
            _session = session;
            _availabilityTicket = new SendQueueAvailabilityTicket(Id);
            _pendingMessages = new();
            _lock = new();
            _queue = Channel.CreateUnbounded<SerializedPayload>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            _maxConcurrencyLevel = 10;
            _sendPrefetchThrottler = new SemaphoreSlim(_maxConcurrencyLevel, _maxConcurrencyLevel);
            _cts = new CancellationTokenSource();
        }
        
        public void ProcessPendingPayloads()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!_stopped)
                {
                    try
                    {
                        await ReadNextPayloadAsync();
                    }
                    catch
                    {
                        // if there is an exception, just ignore
                    }
                }
                
                Logger.LogInformation($"Exited {_session.Id}");
            }, TaskCreationOptions.LongRunning);
        }

        public async Task ReadNextPayloadAsync()
        {
            try
            {
                // read the serialized payload from reader
                var serializedPayload = await _queue.Reader.ReadAsync();
                
                Logger.LogInformation($"send begin for id {serializedPayload.Id}");

                if (serializedPayload.Type == PayloadType.Msg)
                    await SendMessagePayloadAsync(serializedPayload);
                else
                    await SendNonMessagePayloadAsync(serializedPayload);
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
        }

        public void Configure(int prefetchCount, bool autoAck)
        {
            _autoAck = autoAck;
            _maxConcurrencyLevel = prefetchCount;
            _sendPrefetchThrottler.Dispose();
            _sendPrefetchThrottler = new SemaphoreSlim(_maxConcurrencyLevel, prefetchCount);
        }

        /// <summary>
        ///     Stop will send fail message event for all messages in queue and in pending messages list
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
            
            _queue.Writer.Complete();
  
            _stopped = true;

            // foreach pending message, requeue
            foreach (var (_, serializedPayload) in _pendingMessages)
            {
                try
                {
                    Logger.LogInformation($"SendQueue -> nacked message {serializedPayload.Id}");
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    ObjectPool.Shared.Return(serializedPayload);
                }
                catch 
                {
                    // do nothing
                }
            }

            while (_queue.Reader.TryRead(out var serializedPayload))
            {
                try
                {
                    Logger.LogInformation($"SendQueue -> nacked message {serializedPayload.Id}");
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    ObjectPool.Shared.Return(serializedPayload);
                }
                catch 
                {
                    // do nothing
                }
            }

            _pendingMessages.Clear();
        }

        public void Enqueue(SerializedPayload serializedPayload)
        {
            Logger.LogInformation($"SendQueue -> Processing message {serializedPayload.Id}");
            var writeSuccess = _queue.Writer.TryWrite(serializedPayload);
            if (_stopped || !writeSuccess)
            {
                try
                {
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    ObjectPool.Shared.Return(serializedPayload);
                }
                catch 
                {
                    // do nothing
                }
         
            }
        }

        public void OnMessageAckReceived(Guid messageId)
        {
            Logger.LogInformation($"SendQueue -> ack gernela message {messageId}");
            if (_pendingMessages.TryRemove(messageId, out var serializedPayload))
            {
                try
                {
                    Logger.LogInformation($"SendQueue -> ack full message {messageId}");
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Ack);
                    ObjectPool.Shared.Return(serializedPayload);
                    OnAvailable?.Invoke(AvailabilityTicket);
                    _sendPrefetchThrottler.Release();
                }
                catch
                {
                    // do nothing
                }
            }
            else
            {
                Logger.LogInformation($"SendQueue -> ack not released {messageId}");
            }
        }

        public void OnMessageNackReceived(Guid messageId)
        {
            Logger.LogInformation($"SendQueue -> nack gernela message {messageId}");
            if (_pendingMessages.Remove(messageId, out var serializedPayload))
            {
                try
                {
                    serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                    ObjectPool.Shared.Return(serializedPayload);
                    OnAvailable?.Invoke(AvailabilityTicket);
                    _sendPrefetchThrottler.Release();
                }
                catch
                {
                    // do nothing
                }
            }
            else
            {
                Logger.LogInformation($"SendQueue -> nack not released {messageId}");
            }
        }

        private async Task SendMessagePayloadAsync(SerializedPayload serializedPayload)
        {
            try
            {
                Logger.LogInformation($"SendQueue -> before wait {serializedPayload.Id}");
                await _sendPrefetchThrottler.WaitAsync(_cts.Token);
                Logger.LogInformation($"SendQueue -> after wait {serializedPayload.Id}");

                // add message to pending message dict
                _pendingMessages[serializedPayload.Id] = serializedPayload;

                // send the payload 
                var success = await _session.SendAsync(serializedPayload.Data);

                Logger.LogInformation($"SendQueue -> sent message {serializedPayload.Id} with result {success}");

                // notify message ack it is auto ack
                if (success && _autoAck)
                    OnMessageAckReceived(serializedPayload.Id);
                // notify message nack if it was not sent
                else if (!success)
                    OnMessageNackReceived(serializedPayload.Id);
            }
            finally
            {
                // if stopped don't continue
                if (_stopped)
                {
                    
                    try
                    {
                        serializedPayload.SetStatus(SerializedPayloadStatusUpdate.Nack);
                        Logger.LogInformation($"stopped is called in finally for {serializedPayload.Id}");
                        ObjectPool.Shared.Return(serializedPayload);
                        _sendPrefetchThrottler.Release();
                    }
                    catch
                    {
                        // do nothing
                    }
                }
            }
        }

        private async Task SendNonMessagePayloadAsync(SerializedPayload serializedPayload)
        {
            await _session.SendAsync(serializedPayload.Data);

            ObjectPool.Shared.Return(serializedPayload);
        }

        
    }
}