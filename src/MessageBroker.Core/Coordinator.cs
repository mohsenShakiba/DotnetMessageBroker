using System;
using MessageBroker.Common.Logging;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Queues;
using MessageBroker.TCP;
using MessageBroker.TCP.Client;

namespace MessageBroker.Core
{
    public class Coordinator : ISocketEventProcessor, ISocketDataProcessor
    {
        private readonly IPayloadProcessor _payloadProcessor;
        private readonly ISendQueueStore _sendQueueStore;
        private readonly IMessageStore _messageStore;
        private readonly IQueueStore _queueStore;

        public Coordinator(IPayloadProcessor payloadProcessor, ISendQueueStore sendQueueStore, IQueueStore queueStore,
            IMessageStore messageStore)
        {
            _payloadProcessor = payloadProcessor;
            _sendQueueStore = sendQueueStore;
            _queueStore = queueStore;
            _messageStore = messageStore;
        }

        public void Setup()
        {
            _queueStore.Setup();
            _messageStore.Setup();
        }

        public void DataReceived(Guid sessionId, Memory<byte> payloadData)
        {
            try
            {
                _payloadProcessor.OnDataReceived(sessionId, payloadData);
            }
            catch (Exception e)
            {
                Logger.LogError($"An error occured while trying to dispatch messages, error: {e}");
            }
        }

        public void ClientConnected(IClientSession clientSession)
        {
            Logger.LogInformation("client session connected, added send queue");
            _sendQueueStore.Add(clientSession);
        }

        public void ClientDisconnected(IClientSession clientSession)
        {
            Logger.LogInformation("client session disconnected, removing send queue");

            foreach (var queue in _queueStore.GetAll())
                queue.SessionDisconnected(clientSession.Id);

            _sendQueueStore.Remove(clientSession);
        }
    }
}