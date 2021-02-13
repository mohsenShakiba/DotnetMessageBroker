using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.Queues;
using MessageBroker.Serialization;
using MessageBroker.Socket.Client;

namespace MessageBroker.Core
{
    /// <summary>
    ///     SendQueue is in charge of sending messages to subscribers
    ///     it will handle how many messages has been sent based on the concurrency requested
    /// </summary>
    public class SendQueue
    {
        private readonly Channel<MessagePayload> _messageQueue;
        private readonly ConcurrentDictionary<Guid, MessagePayload> _pendingMessages;
        private readonly Channel<SerializedPayload> _queue;
        private readonly SemaphoreSlim _semaphore;
        private bool _autoAck;
        private int _currentConcurrncyLevel;

        private SemaphoreSlim _sendSemaphore;
        private bool _stopped;

        public SendQueue(IClientSession session)
        {
            Session = session;
            _queue = Channel.CreateUnbounded<SerializedPayload>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _messageQueue = Channel.CreateUnbounded<MessagePayload>();
            _semaphore = new SemaphoreSlim(1, 1);
            _pendingMessages = new ConcurrentDictionary<Guid, MessagePayload>();

            _currentConcurrncyLevel = 10;
            _sendSemaphore = new SemaphoreSlim(_currentConcurrncyLevel, _currentConcurrncyLevel);

            SetupQueueRunner();
            SetupMessageQueueRunner();
        }


        public IClientSession Session { get; }


        private void SetupQueueRunner()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                    try
                    {
                        // if stopped, ignore
                        if (_stopped)
                            return;

                        // read the payload from reader
                        var msg = await _queue.Reader.ReadAsync();

                        // send to socket client
                        await Send(msg);
                    }
                    catch
                    {
                        // if there is an exception, just ignore
                    }
            }, TaskCreationOptions.LongRunning);
        }

        private void SetupMessageQueueRunner()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                    try
                    {
                        // if stopped, ignore
                        if (_stopped)
                            return;


                        await _sendSemaphore.WaitAsync();

                        // read the message from reader
                        var msg = await _messageQueue.Reader.ReadAsync();

                        // send to socket client
                        await Send(msg);
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
            _sendSemaphore = new SemaphoreSlim(concurrency - (_currentConcurrncyLevel - _sendSemaphore.CurrentCount),
                concurrency);
            _currentConcurrncyLevel = concurrency;
        }

        /// <summary>
        ///     Stop will send fail message event for all messages in queue and in pending messages list
        /// </summary>
        public void Stop()
        {
            _stopped = true;

            // send fail message event 
            while (_messageQueue.Reader.TryRead(out var queueSendPayload))
            {
                queueSendPayload.SetStatus(MessagePayloadStatus.Nack);
                queueSendPayload.Dispose();
            }

            // dispose payloads
            while (_queue.Reader.TryRead(out var sendPayload))
                // return the send payload
                ObjectPool.Shared.Return(sendPayload);

            // foreach pending message, requeue
            foreach (var (_, pendingQueueSendPayload) in _pendingMessages)
            {
                pendingQueueSendPayload.SetStatus(MessagePayloadStatus.Nack);
                pendingQueueSendPayload.Dispose();
            }
        }

        public void Enqueue(MessagePayload sendPayload)
        {
            _messageQueue.Writer.TryWrite(sendPayload);
        }

        public void Enqueue(SerializedPayload serializedPayload)
        {
            _queue.Writer.TryWrite(serializedPayload);
        }

        public void OnMessageAckReceived(Guid messageId)
        {
            if (_pendingMessages.TryRemove(messageId, out var queueSendPayload))
            {
                queueSendPayload.SetStatus(MessagePayloadStatus.Ack);
                _sendSemaphore.Release();
                queueSendPayload.Dispose();
            }
        }

        public void OnMessageNackReceived(Guid messageId)
        {
            if (_pendingMessages.Remove(messageId, out var queueSendPayload))
            {
                queueSendPayload.SetStatus(MessagePayloadStatus.Nack);
                _sendSemaphore.Release();
                queueSendPayload.Dispose();
            }
        }

        private async Task Send(MessagePayload messagePayload)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (!_autoAck) _pendingMessages[messagePayload.SerializedPayload.Id] = messagePayload;

                // send the payload 
                var success = await Session.SendAsync(messagePayload.SerializedPayload.Data);

                // notify message was sent or failed
                if (success)
                    // if auto ack is active, then mark as acked
                    if (_autoAck)
                    {
                        messagePayload.SetStatus(MessagePayloadStatus.Ack);

                        // return send payload to shared pool
                        messagePayload.Dispose();

                        _sendSemaphore.Release();
                    }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task Send(SerializedPayload serializedPayload)
        {
            await _semaphore.WaitAsync();

            try
            {
                // send the payload 
                var success = await Session.SendAsync(serializedPayload.Data);

                // notify message was sent or failed
                if (success)
                    // return send payload to shared pool
                    ObjectPool.Shared.Return(serializedPayload);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}