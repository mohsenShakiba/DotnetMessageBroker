using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core
{
    /// <summary>
    /// MessageDispatcher is in charge of dispatching the message to the appropriate SendQueue
    /// </summary>
    public class MessageDispatcher
    {
        private readonly ISessionResolver _sessionResolver;
        private readonly ISerializer _serializer;
        private readonly ConcurrentDictionary<Guid, SendQueue> _sendQueues;


        public MessageDispatcher(ISessionResolver sessionResolver, ISerializer serializer)
        {
            _sessionResolver = sessionResolver;
            _serializer = serializer;
            _sendQueues = new();
        }

        /// <summary>
        /// returns the SendQueue based on SessionId
        /// it's required for testing
        /// </summary>
        /// <param name="sessionId">Id of ClientSession</param>
        /// <returns></returns>
        public SendQueue GetSendQueue(Guid sessionId)
        {
            if (_sendQueues.TryGetValue(sessionId, out var sendQueue))
            {
                return sendQueue;
            }

            return null;
        }

        /// <summary>
        /// AddSendQueue will create and store a new SendQueue will the specified concurrency
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="concurrency"></param>
        public void AddSendQueue(Guid sessionId, int concurrency)
        {
            var session = _sessionResolver.Resolve(sessionId);
            if (session != null)
            {
                var sendQueue = new SendQueue(session, _serializer, concurrency);
                _sendQueues[sessionId] = sendQueue;
            }
        }

        /// <summary>
        /// Dispatch will send the message to all the destination
        /// if a SendQueue is found for a session id then the Enqueue method will be called
        /// otherwise a SendQueue will be created and Enqueue will be called
        /// </summary>
        /// <param name="msg">The message that must be dispatched</param>
        /// <param name="destinations">Array of Guids that the message must be dispatched to</param>
        public void Dispatch(Message msg, IEnumerable<Guid> destinations)
        {
            foreach(var destination in destinations)
            {
                if (_sendQueues.TryGetValue(destination, out var sendQueue))
                {
                    sendQueue.Enqueue(msg);
                }
            }
        }

        /// <summary>
        /// release will dispatch the message id to the appropriate SendQueues
        /// </summary>
        /// <param name="messageId">MessageId that has been acked or nacked</param>
        /// <param name="destinations">SenQueues that must receive this message id</param>
        public void Release(Guid messageId, Guid[] destinations)
        {
            foreach(var destination in destinations)
            {
                if (_sendQueues.TryGetValue(destination, out var sendQueue))
                {
                    sendQueue.ReleaseOne(messageId);
                }
            }
        }

    }
}
