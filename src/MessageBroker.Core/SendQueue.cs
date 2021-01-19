using MessageBroker.Core.Serialize;
using MessageBroker.SocketServer.Abstractions;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Core.Payloads;
using MessageBroker.Core.Pools;

namespace MessageBroker.Core
{
    /// <summary>
    /// SendQueue is in charge of sending messages to subscribers
    /// it will handle how many messages has been sent based on the concurrency requested
    /// </summary>
    public class SendQueue
    {
        private readonly IClientSession _session;
        private readonly ISerializer _serializer;
        private readonly ConcurrentQueue<SendPayload> _queue;
        private readonly ConcurrentBag<Guid> _pendingMessages;

        private int _currentConcurrency;
        private int _maxConcurrency;
        private bool _isSending;
        private object _lock;

        private bool IsQueueFull => _currentConcurrency >= _maxConcurrency;
        private bool IsQueueEmpty => _currentConcurrency == 0;

        public int CurrentConcurrency => _currentConcurrency;
        public int MaxConcurrency => _maxConcurrency;
        public IClientSession Session => _session;


        public SendQueue(IClientSession session, ISerializer serializer)
        {
            _session = session;
            _serializer = serializer;
            _queue = new();
            _pendingMessages = new();
            _lock = new();

            // setup the method that is used when the sending of asynchronous data is compelete
            _session.SetupSendCompletedHandler(SendPendingMessagesIfQueueNotFull);
        }

        public void SetupConcurrency(int maxConcurrency, int currentConcurrency = 0)
        {
            _maxConcurrency = maxConcurrency;
        }

        /// <summary>
        /// Enqueue will send the message to subscriber immediately if the queue isn't full
        /// otherwise the message will be saved in a queue to be sent when the queue isn't full
        /// </summary>
        /// <param name="message">The message that must be sent</param>
        public void Enqueue(Message message)
        {
            var sendPayload = _serializer.ToSendPayload(message);

            // the queue shouldn't have any item in it and the send queue must not be in sending mode
            if (_isSending || !IsQueueEmpty)
            {
                _queue.Enqueue(sendPayload);
            }
            else
            {
                Send(sendPayload);
            }
        }

        public void Enqueue(Ack ack)
        {
            var sendPayload = _serializer.ToSendPayload(ack);

            // note: we don't need to check for IsQueueFull since ack can be sent regardless of queue size
            if (_isSending)
            {
                _queue.Enqueue(sendPayload);
            }
            else
            {
                Send(sendPayload);
            }
        }

        /// <summary>
        /// ReleaseOne will remove the message from pending messages array, clearing room in the queue
        /// </summary>
        /// <param name="messageId">The id of message that was acked or nacked</param>
        public void ReleaseOne(Guid messageId)
        {
            if (_pendingMessages.Contains(messageId))
            {
                Interlocked.Decrement(ref _currentConcurrency);

                if (_isSending)
                    return;

                SendPendingMessagesIfQueueNotFull();
            }
        }

        /// <summary>
        /// This method will send another message if the queue isn't full
        /// This method will be called when ReleaseOne is called
        /// </summary>
        private void SendPendingMessagesIfQueueNotFull()
        {
            lock (_lock)
            {
                _isSending = false;

                if (IsQueueFull)
                    return;

                if (_queue.TryDequeue(out var msg))
                    Send(msg);
            }
        }

        /// <summary>
        /// Send will send the payload to ClientSession
        /// it will also increment the _currentConcurrency and add the message to _pendingMessages list
        /// </summary>
        /// <param name="sendPayload"></param>
        private void Send(SendPayload sendPayload)
        {
            lock (_lock)
            {
                // perform the following actions only when payload type is msg
                if (sendPayload.IsMessageType)
                {
                    // increment the _currentConcurrency
                    Interlocked.Increment(ref _currentConcurrency);

                    // add the id of the payload to list of pending 
                    _pendingMessages.Add(sendPayload.Id);
                }

                // send the payload 
                var sendAsync = _session.SendAsync(sendPayload.Data);

                // return the send queue to pool
                ObjectPool.Shared.Return(sendPayload);

                // if the message isn't sent asynchronously
                if (!sendAsync)
                {
                    // check if any pending messages exist
                    SendPendingMessagesIfQueueNotFull();
                }
                // otherwise wait for the message to be sent
                else
                {
                    // set is loading to true
                    // so no longer messages will be sent while in progress
                    _isSending = true;
                }
            }
        }
    }
}