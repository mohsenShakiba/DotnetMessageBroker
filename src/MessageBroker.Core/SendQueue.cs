using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using MessageBroker.Models.Models;
using MessageBroker.Serialization;
using MessageBroker.Serialization.Pools;
using MessageBroker.SocketServer.Abstractions;

namespace MessageBroker.Core
{
    /// <summary>
    ///     SendQueue is in charge of sending messages to subscribers
    ///     it will handle how many messages has been sent based on the concurrency requested
    /// </summary>
    public class SendQueue
    {
        private readonly ConcurrentBag<Guid> _pendingMessages;
        private readonly ConcurrentQueue<SendPayload> _queue;
        private readonly ISerializer _serializer;

        private int _currentConcurrency;
        private bool _isSending;
        private readonly object _lock;


        public SendQueue(IClientSession session, ISerializer serializer)
        {
            Session = session;
            _serializer = serializer;
            _queue = new ConcurrentQueue<SendPayload>();
            _pendingMessages = new ConcurrentBag<Guid>();
            _lock = new object();

            // setup the method that is used when the sending of asynchronous data is compelete
            Session.SetupSendCompletedHandler(SendPendingMessagesIfQueueNotFull);
        }

        private bool IsQueueFull => _currentConcurrency >= MaxConcurrency;
        private bool IsQueueEmpty => _currentConcurrency == 0;

        public int CurrentConcurrency => _currentConcurrency;
        public int MaxConcurrency { get; private set; }

        public IClientSession Session { get; }

        public void SetupConcurrency(int maxConcurrency, int currentConcurrency = 0)
        {
            MaxConcurrency = maxConcurrency;
        }

        /// <summary>
        ///     Enqueue will send the message to subscriber immediately if the queue isn't full
        ///     otherwise the message will be saved in a queue to be sent when the queue isn't full
        /// </summary>
        /// <param name="message">The message that must be sent</param>
        public void Enqueue(Message message)
        {
            var sendPayload = _serializer.ToSendPayload(message);

            // the queue shouldn't have any item in it and the send queue must not be in sending mode
            if (_isSending || !IsQueueEmpty)
                _queue.Enqueue(sendPayload);
            else
                Send(sendPayload);
        }

        public void Enqueue(Ack ack)
        {
            var sendPayload = _serializer.ToSendPayload(ack);

            // note: we don't need to check for IsQueueFull since ack can be sent regardless of queue size
            if (_isSending)
                _queue.Enqueue(sendPayload);
            else
                Send(sendPayload);
        }

        /// <summary>
        ///     ReleaseOne will remove the message from pending messages array, clearing room in the queue
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
        ///     This method will send another message if the queue isn't full
        ///     This method will be called when ReleaseOne is called
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
        ///     Send will send the payload to ClientSession
        ///     it will also increment the _currentConcurrency and add the message to _pendingMessages list
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
                var sendAsync = Session.SendAsync(sendPayload.Data);

                // return the send queue to pool
                ObjectPool.Shared.Return(sendPayload);

                // if the message isn't sent asynchronously
                if (!sendAsync)
                    // check if any pending messages exist
                    SendPendingMessagesIfQueueNotFull();
                // otherwise wait for the message to be sent
                else
                    // set is loading to true
                    // so no longer messages will be sent while in progress
                    _isSending = true;
            }
        }
    }
}