﻿using System;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.TaskManager;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    public class ReceiveDataProcessor : IReceiveDataProcessor
    {
        private readonly IQueueConsumerCoordinator _queueConsumerCoordinator;
        private readonly ISendPayloadTaskManager _sendPayloadTaskManager;
        private readonly ISerializer _serializer;

        public ReceiveDataProcessor(ISerializer serializer,
            IQueueConsumerCoordinator queueConsumerCoordinator, ISendPayloadTaskManager sendPayloadTaskManager)
        {
            _serializer = serializer;
            _queueConsumerCoordinator = queueConsumerCoordinator;
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
                default:
                    throw new InvalidOperationException(
                        "Failed to map type to appropriate action while parsing payload");
            }
        }

        private void OnMessage(Memory<byte> payloadData)
        {
            var queueMessage = _serializer.ToQueueMessage(payloadData);
            _queueConsumerCoordinator.OnMessage(queueMessage);
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
    }
}