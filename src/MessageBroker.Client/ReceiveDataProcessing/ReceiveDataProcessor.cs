using System;
using System.Runtime.CompilerServices;
using System.Threading;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Client.Subscriptions.Store;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Models;
using MessageBroker.Common.Serialization;
using MessageBroker.Common.Tcp.EventArgs;

[assembly: InternalsVisibleTo("Tests")]

namespace MessageBroker.Client.ReceiveDataProcessing
{
    /// <inheritdoc />
    public class ReceiveDataProcessor : IReceiveDataProcessor
    {
        private readonly IDeserializer _deserializer;
        private readonly ISubscriptionStore _subscriptionStore;
        private readonly ITaskManager _taskManager;


        private int _receivedMessagesCount;

        /// <summary>
        /// Instantiates a new <see cref="ReceiveDataProcessor" />
        /// </summary>
        /// <param name="deserializer"></param>
        /// <param name="subscriptionStore"></param>
        /// <param name="taskManager"></param>
        public ReceiveDataProcessor(IDeserializer deserializer,
            ISubscriptionStore subscriptionStore, ITaskManager taskManager)
        {
            _deserializer = deserializer;
            _subscriptionStore = subscriptionStore;
            _taskManager = taskManager;
        }

        /// <inheritdoc />
        public event Action<Guid> OnOkReceived;

        /// <inheritdoc />
        public event Action<Guid, string> OnErrorReceived;

        /// <inheritdoc />
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