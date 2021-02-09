using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.EventStores;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Client.ReceiveDataProcessing
{
    public class ReceiveDataProcessor : IReceiveDataProcessor
    {
        private readonly IBinaryDataProcessor _binaryDataProcessor;
        private readonly ISerializer _serializer;
        private readonly IQueueConsumerCoordinator _queueConsumerCoordinator;
        private readonly ITaskManager _taskManager;

        public ReceiveDataProcessor(IBinaryDataProcessor binaryDataProcessor, ISerializer serializer,
            IQueueConsumerCoordinator queueConsumerCoordinator, ITaskManager taskManager)
        {
            _binaryDataProcessor = binaryDataProcessor;
            _serializer = serializer;
            _queueConsumerCoordinator = queueConsumerCoordinator;
            _taskManager = taskManager;
        }

        public void AddReceiveDataChunk(Memory<byte> binaryChunk)
        {
            _binaryDataProcessor.Write(binaryChunk);
            ProcessDate();
        }

        private void ProcessDate()
        {
            while (true)
            {
                if (_binaryDataProcessor.TryRead(out var binaryData))
                {
                    ParsePayload(binaryData.DataWithoutSize);
                    ObjectPool.Shared.Return(binaryData);
                }
                else
                {
                    break;
                }
            }
        }

        private void ParsePayload(Memory<byte> payloadData)
        {
            var payloadType = _serializer.ParsePayloadType(payloadData);
            switch (payloadType)
            {
                case PayloadType.Ack:
                    OnAck(payloadData);
                    break;
                case PayloadType.Nack:
                    OnError(payloadData);
                    break;
                case PayloadType.Msg:
                    OnMessage(payloadData);
                    break;
                default:
                    throw new InvalidOperationException(
                        "Failed to map type to appropriate action while parsing payload");
            }
        }

        private void OnMessage(Memory<byte> payloadData)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var queueMessage = _serializer.ToQueueMessage(payloadData);
                _queueConsumerCoordinator.OnMessage(queueMessage);
            });
        }

        private void OnAck(Memory<byte> payloadData)
        {
            var ack = _serializer.ToAck(payloadData);
            _taskManager.OnPayloadEvent(ack.Id, SendEventType.Ack, null);
        }

        private void OnError(Memory<byte> payloadData)
        {
            var nack = _serializer.ToError(payloadData);
            _taskManager.OnPayloadEvent(nack.Id, SendEventType.Nack, nack.Message);
        }

    }
}