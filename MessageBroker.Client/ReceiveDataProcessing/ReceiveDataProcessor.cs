﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Logging;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    public class ReceiveDataProcessor : IReceiveDataProcessor
    {
        private readonly ISubscriberStore _subscriberStore;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly ISerializer _serializer;

        public event Action OnReadyReceived;

        private int _receivedMessagesCount;

        public ReceiveDataProcessor(ISerializer serializer,
            ISubscriberStore subscriberStore, ISendPayloadTaskManager sendPayloadTaskManager)
        {
            _serializer = serializer;
            _subscriberStore = subscriberStore;
            _sendPayloadTaskManager = sendPayloadTaskManager;
        }

        public void DataReceived(Guid sessionId, Memory<byte> data)
        {
            var payloadType = _serializer.ParsePayloadType(data);
            switch (payloadType)
            {
                case PayloadType.Ok:
                    OnOk(data);
                    break;
                case PayloadType.Error:
                    OnError(data);
                    break;
                case PayloadType.Msg:
                    OnMessage(data);
                    break;
                case PayloadType.Ready:
                    OnReady();
                    break;
                default:
                    throw new InvalidOperationException(
                        "Failed to map type to appropriate action while parsing payload");
            }
        }

        private void OnMessage(Memory<byte> payloadData)
        {
            Interlocked.Increment(ref _receivedMessagesCount);
            var queueMessage = _serializer.ToQueueMessage(payloadData);
            _subscriberStore.OnMessage(queueMessage);
        }

        private void OnOk(Memory<byte> payloadData)
        {
            var ack = _serializer.ToAck(payloadData);
            _sendPayloadTaskManager.OnPayloadOkResult(ack.Id);
        }

        private void OnError(Memory<byte> payloadData)
        {
            var nack = _serializer.ToError(payloadData);
            _sendPayloadTaskManager.OnPayloadErrorResult(nack.Id, nack.Message);
        }

        private void OnReady()
        {
            OnReadyReceived?.Invoke();
        }
    }
}