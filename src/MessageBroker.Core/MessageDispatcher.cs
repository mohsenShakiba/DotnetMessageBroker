using System;
using System.Collections.Concurrent;
using MessageBroker.Core.Queues;
using MessageBroker.Serialization;
using MessageBroker.Socket.Client;

namespace MessageBroker.Core
{
    /// <summary>
    ///     MessageDispatcher is in charge of dispatching the message to the appropriate SendQueue
    /// </summary>
    public class MessageDispatcher
    {
        private readonly ConcurrentDictionary<Guid, SendQueue> _sendQueues;


        public MessageDispatcher()
        {
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

        public void AddSession(IClientSession clientSession)
        {
            var sendQueue = new SendQueue(clientSession);
            _sendQueues[clientSession.Id] = sendQueue;
        }

        public void RemoveSession(IClientSession clientSession)
        {
            if (_sendQueues.TryRemove(clientSession.Id, out var sendQueue))
            {
                sendQueue.Stop();
                clientSession.Close();
            }
        }

        public void ConfigureSubscription(Guid sessionId, int concurrency, bool autoAck)
        {
            if (_sendQueues.TryGetValue(sessionId, out var sendQueue))
                sendQueue.Configure(concurrency, autoAck);
        }

        public void Dispatch(SerializedPayload serializedPayload, Guid destination)
        {
            if (_sendQueues.TryGetValue(destination, out var sendQueue))
                sendQueue.Enqueue(serializedPayload);
        }

        public void Dispatch(MessagePayload sendPayload, Guid destination)
        {
            if (_sendQueues.TryGetValue(destination, out var sendQueue))
                sendQueue.Enqueue(sendPayload);
        }


        public void OnMessageAck(Guid messageId, Guid destination)
        {
            if (_sendQueues.TryGetValue(destination, out var sendQueue))
                sendQueue.OnMessageAckReceived(messageId);
        }

        public void OnMessageNack(Guid messageId, Guid destination)
        {
            if (_sendQueues.TryGetValue(destination, out var sendQueue))
                sendQueue.OnMessageNackReceived(messageId);
        }
    }
}