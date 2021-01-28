using System;
using System.Collections.Concurrent;
using MessageBroker.Core.InternalEventChannel;
using MessageBroker.Serialization;
using MessageBroker.SocketServer.Abstractions;

namespace MessageBroker.Core
{
    /// <summary>
    ///     MessageDispatcher is in charge of dispatching the message to the appropriate SendQueue
    /// </summary>
    public class MessageDispatcher
    {
        private readonly ConcurrentDictionary<Guid, SendQueue> _sendQueues;
        private readonly ISessionResolver _sessionResolver;
        private readonly IEventChannel _eventChannel;


        public MessageDispatcher(ISessionResolver sessionResolver, IEventChannel eventChannel)
        {
            _sessionResolver = sessionResolver;
            _eventChannel = eventChannel;
            _sendQueues = new ConcurrentDictionary<Guid, SendQueue>();
        }

        /// <summary>
        ///     returns the SendQueue based on SessionId
        ///     it's required for testing
        /// </summary>
        /// <param name="sessionId">Id of ClientSession</param>
        /// <returns></returns>
        public SendQueue GetSendQueue(Guid sessionId)
        {
            if (_sendQueues.TryGetValue(sessionId, out var sendQueue)) return sendQueue;

            return null;
        }

        /// <summary>
        ///     AddSendQueue will create and store a new SendQueue
        /// </summary>
        /// <param name="sessionId"></param>
        public void AddSendQueue(Guid sessionId)
        {
            var session = _sessionResolver.Resolve(sessionId);
            if (session != null)
            {
                var sendQueue = new SendQueue(session, _eventChannel);
                _sendQueues[sessionId] = sendQueue;
            }
        }

        public void RemoveSendQueue(Guid sessionId)
        {
            if (_sendQueues.TryRemove(sessionId, out var sendQueue))
            {
                sendQueue.Stop();
            }
        }

        public void ConfigureSubscription(Guid sessionId, int concurrency, bool autoAck)
        {
            if (_sendQueues.TryGetValue(sessionId, out var sendQueue))
                sendQueue.Configure(concurrency, autoAck);
        }

        public void Dispatch(SendPayload sendPayload, Guid destination)
        {
            if (_sendQueues.TryGetValue(destination, out var sendQueue))
                sendQueue.Enqueue(sendPayload);
        }


        /// <summary>
        ///     release will dispatch the message id to the appropriate SendQueues
        /// </summary>
        /// <param name="messageId">MessageId that has been acked or nacked</param>
        /// <param name="destinations">SenQueues that must receive this message id</param>
        public void Release(Guid messageId, Guid destination)
        {
            if (_sendQueues.TryGetValue(destination, out var sendQueue))
                sendQueue.ReleaseOne(messageId);
        }
    }
}