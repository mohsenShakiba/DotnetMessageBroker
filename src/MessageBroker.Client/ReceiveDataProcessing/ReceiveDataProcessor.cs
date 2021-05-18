using System;
using System.Threading;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Client.Subscriptions.Store;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Models;
using MessageBroker.Common.Serialization;
using MessageBroker.Common.Tcp.EventArgs;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    public class ReceiveDataProcessor : IReceiveDataProcessor
    {
        private readonly IDeserializer _deserializer;
        private readonly ITaskManager _taskManager;
        private readonly ISubscriptionStore _subscriptionStore;


        private int _receivedMessagesCount;

        public ReceiveDataProcessor(IDeserializer deserializer,
            ISubscriptionStore subscriptionStore, ITaskManager taskManager)
        {
            _deserializer = deserializer;
            _subscriptionStore = subscriptionStore;
            _taskManager = taskManager;
        }

        public event Action<Guid> OnOkReceived;
        public event Action<Guid, string> OnErrorReceived;

        public void DataReceived(object clientSessionObject, ClientSessionDataReceivedEventArgs dataReceivedEventArgs)
        {
            var data = dataReceivedEventArgs.Data;
            var payloadType = _deserializer.ParsePayloadType(data);
            switch (payloadType)
            {
                case PayloadType.Ok:
                    OnOk(data);
                    break;
                case PayloadType.Error:
                    OnError(data);
                    break;
                case PayloadType.TopicMessage:
                    OnMessage(data);
                    break;
                default:
                    throw new InvalidOperationException(
                        "Failed to map type to appropriate action while parsing payload");
            }
        }


        private void OnMessage(Memory<byte> payloadData)
        {
            Interlocked.Increment(ref _receivedMessagesCount);
            var queueMessage = _deserializer.ToTopicMessage(payloadData);
            if (_subscriptionStore.TryGet(queueMessage.TopicName, out var subscription))
                ((Subscription) subscription).OnMessageReceived(queueMessage);
        }

        private void OnOk(Memory<byte> payloadData)
        {
            var ack = _deserializer.ToAck(payloadData);
            _taskManager.OnPayloadOkResult(ack.Id);
            OnOkReceived?.Invoke(ack.Id);
        }

        private void OnError(Memory<byte> payloadData)
        {
            var nack = _deserializer.ToError(payloadData);
            _taskManager.OnPayloadErrorResult(nack.Id, nack.Message);
            OnErrorReceived?.Invoke(nack.Id, nack.Message);
        }
    }
}