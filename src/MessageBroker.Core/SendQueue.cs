using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.InternalEventChannel;
using MessageBroker.Serialization;
using MessageBroker.SocketServer.Abstractions;

namespace MessageBroker.Core
{
    /// <summary>
    ///     SendQueue is in charge of sending messages to subscribers
    ///     it will handle how many messages has been sent based on the concurrency requested
    /// </summary>
    public class SendQueue
    {
        private readonly IEventChannel _eventChannel;
        private readonly List<Guid> _pendingMessageIds;
        private readonly Channel<SendPayload> _queue;
        private readonly Channel<SendPayload> _messageQueue;
        private readonly SemaphoreSlim _semaphore;

        private int _currentConcurrency;
        private int _maxConcurrency;
        private bool _autoAck;
        private bool _stopped;

        public bool IsQueueFull => _currentConcurrency >= MaxConcurrency;
        private bool IsUnlimitedConcurrency => _maxConcurrency == -1;
        
        public int CurrentConcurrency => _currentConcurrency;
        public int MaxConcurrency => _maxConcurrency;
        

        public IClientSession Session { get; }

        public SendQueue(IClientSession session, IEventChannel eventChannel)
        {
            _eventChannel = eventChannel;
            Session = session;
            _queue = Channel.CreateUnbounded<SendPayload>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                
            });
            _messageQueue = Channel.CreateUnbounded<SendPayload>();
            _pendingMessageIds = new();
            _maxConcurrency = -1; // no limit on concurrency
            _semaphore = new(1, 1);

            SetupQueueRunner();
            SetupMessageQueueRunner();
            
            // setup the method that is used when the sending of asynchronous data is compelete
            Session.SetupSendCompletedHandler(OnMessageSent, OnMessageError);
        }

        private void SetupQueueRunner()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        // if stopped, ignore
                        if (_stopped)
                            return;

                        // read the payload from reader
                        var msg = await _queue.Reader.ReadAsync();
                        
                        // wait for reset event
                        await _semaphore.WaitAsync();
                        
                        Send(msg);
                    }
                    catch
                    {
                        // if there is an exception, just ignore
                    }
                }
            });
        }

        private void SetupMessageQueueRunner()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        // if stopped, ignore
                        if (_stopped)
                            return;

                        // if queue is full then wait for a little bit
                        if (IsQueueFull && !IsUnlimitedConcurrency)
                        {
                            // this is not a magic number, just something that performed well in tests
                            await Task.Delay(50);
                            continue;
                        }

                        // read the message from reader
                        var msg = await _messageQueue.Reader.ReadAsync();
                        
                        // wait for reset event
                        await _semaphore.WaitAsync();
                        
                        Send(msg);
                    }
                    catch
                    {
                        // if there is an exception, just ignore
                    }
                }
            });
        }

        public void Configure(int concurrency, bool autoAck, int currentConcurrency = 0)
        {
            _maxConcurrency = concurrency;
            _currentConcurrency = currentConcurrency;
            _autoAck = autoAck;
        }

        /// <summary>
        /// Stop will send fail message event for all messages in queue and in pending messages list
        /// </summary>
        public void Stop()
        {
            _stopped = true;

            // send fail message event 
            while (_messageQueue.Reader.TryRead(out var sendPayload))
            {
                _eventChannel.OnMessageError(Session.SessionId, sendPayload.Id);

                // return the send payload
                ObjectPool.Shared.Return(sendPayload);
            }
            
            // dispose payloads
            while (_queue.Reader.TryRead(out var sendPayload))
            {
                // return the send payload
                ObjectPool.Shared.Return(sendPayload);
            }

            // foreach pending message, requeue
            foreach (var pendingMessageId in _pendingMessageIds)
            {
                _eventChannel.OnMessageError(Session.SessionId, pendingMessageId);
            }
        }

        public void Enqueue(SendPayload sendPayload)
        {
            if (sendPayload.IsMessageType)
            {
                _messageQueue.Writer.TryWrite(sendPayload);
            }
            else
            {
                _queue.Writer.TryWrite(sendPayload);
            }
        }

        /// <summary>
        ///     ReleaseOne will remove the message from pending messages array, clearing room in the queue
        /// </summary>
        /// <param name="messageId">The id of message that was acked or nacked</param>
        public void ReleaseOne(Guid messageId)
        {
            if (_pendingMessageIds.Remove(messageId))
            {
                Interlocked.Decrement(ref _currentConcurrency);
            }
        }

        /// <summary>
        /// this method is called once the message is sent by the client session
        /// </summary>
        private void OnMessageSent(Guid messageId)
        {
            // mark message as sent
            _eventChannel.OnMessageSent(Session.SessionId, messageId, _autoAck);
            
            _semaphore.Release();
        }

        private void OnMessageError(Guid messageId)
        {
            _eventChannel.OnMessageError(Session.SessionId, messageId);
        }

        /// <summary>
        ///     Send will send the payload to ClientSession
        ///     it will also increment the _currentConcurrency and add the message to _pendingMessages list
        /// </summary>
        /// <param name="sendPayload"></param>
        private void Send(SendPayload sendPayload)
        {
            // perform the following actions only when payload type is msg
            if (sendPayload.IsMessageType)
            {
                // increment the _currentConcurrency
                Interlocked.Increment(ref _currentConcurrency);

                // add the id of the payload to list of pending 
                _pendingMessageIds.Add(sendPayload.Id);
            }

            // send the payload 
            var sendAsync = Session.SendAsync(sendPayload.Data);

            // setup send payload
            Session.SetSendPayloadId(sendPayload.Id);

            // if the message isn't sent asynchronously
            if (!sendAsync)
                // check if any pending messages exist
                OnMessageSent(sendPayload.Id);
            
            // return send payload to shared pool
            ObjectPool.Shared.Return(sendPayload);
        }
    }
}