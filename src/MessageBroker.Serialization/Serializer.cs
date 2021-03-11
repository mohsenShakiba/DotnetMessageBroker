using System;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Models.BinaryPayload;

namespace MessageBroker.Serialization
{
    public class Serializer : ISerializer
    {
        public bool IsReturnedToPool { get; private set; }

        public PayloadType ParsePayloadType(Memory<byte> b)
        {
            var messageType =
                (PayloadType) BitConverter.ToInt32(b.Slice(0, BinaryProtocolConfiguration.PayloadHeaderSize).Span);
            return messageType;
        }
        
        #region Serialize

        public SerializedPayload Serialize(Message msg)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Msg)
                .WriteId(msg.Id)
                .WriteStr(msg.Route)
                .WriteMemory(msg.Data)
                .Build();
        }

        public SerializedPayload Serialize(QueueMessage msg)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Msg)
                .WriteId(msg.Id)
                .WriteStr(msg.Route)
                .WriteStr(msg.QueueName)
                .WriteMemory(msg.Data)
                .Build();
        }

        public SerializedPayload Serialize(Ack ack)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Ack)
                .WriteId(ack.Id)
                .Build();
        }

        public SerializedPayload Serialize(Nack ack)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Nack)
                .WriteId(ack.Id)
                .Build();
        }

        public SerializedPayload Serialize(Ok ok)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Ok)
                .WriteId(ok.Id)
                .Build();
        }

        public SerializedPayload Serialize(Error error)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Error)
                .WriteId(error.Id)
                .WriteStr(error.Message)
                .Build();
        }

        public SerializedPayload Serialize(SubscribeQueue subscribeQueue)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.SubscribeQueue)
                .WriteId(subscribeQueue.Id)
                .WriteStr(subscribeQueue.QueueName)
                .Build();
        }

        public SerializedPayload Serialize(UnsubscribeQueue subscribeQueue)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.UnSubscribeQueue)
                .WriteId(subscribeQueue.Id)
                .WriteStr(subscribeQueue.QueueName)
                .Build();
        }

        public SerializedPayload Serialize(QueueDeclare queueDeclare)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.QueueCreate)
                .WriteId(queueDeclare.Id)
                .WriteStr(queueDeclare.Name)
                .WriteStr(queueDeclare.Route)
                .Build();
        }

        public SerializedPayload Serialize(QueueDelete queueDelete)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.QueueDelete)
                .WriteId(queueDelete.Id)
                .WriteStr(queueDelete.Name)
                .Build();
        }

        public SerializedPayload Serialize(ConfigureSubscription configureSubscription)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.ConfigureSubscription)
                .WriteId(configureSubscription.Id)
                .WriteInt(configureSubscription.Concurrency)
                .WriteInt(configureSubscription.AutoAck ? 1 : 0)
                .Build();
        }

        public SerializedPayload Serialize(Ready ready)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Ready)
                .Build();
        }

        #endregion

        #region Deserialize

        public Ack ToAck(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var messageId = receivePayload.ReadNextGuid();
                return new Ack {Id = messageId};
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public Nack ToNack(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var messageId = receivePayload.ReadNextGuid();
                return new Nack {Id = messageId};
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public Ok ToOk(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var message = receivePayload.ReadNextString();

                return new Ok
                {
                    Id = id
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }


        public Error ToError(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var message = receivePayload.ReadNextString();

                return new Error
                {
                    Id = id,
                    Message = message
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public Message ToMessage(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var messageId = receivePayload.ReadNextGuid();
                var route = receivePayload.ReadNextString();
                var dataSize = receivePayload.ReadNextBytes();

                return new Message
                {
                    Id = messageId,
                    Route = route,
                    Data = dataSize.OriginalData.AsMemory(0, dataSize.Size),
                    OriginalMessageData = dataSize.OriginalData
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public QueueMessage ToQueueMessage(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var messageId = receivePayload.ReadNextGuid();
                var route = receivePayload.ReadNextString();
                var queueName = receivePayload.ReadNextString();
                var dataSize = receivePayload.ReadNextBytes();

                return new QueueMessage
                {
                    Id = messageId,
                    Route = route,
                    QueueName = queueName,
                    Data = dataSize.OriginalData.AsMemory(0, dataSize.Size),
                    OriginalMessageData = dataSize.OriginalData
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public SubscribeQueue ToSubscribeQueue(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var queueName = receivePayload.ReadNextString();

                return new SubscribeQueue
                {
                    Id = id,
                    QueueName = queueName
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public UnsubscribeQueue ToUnsubscribeQueue(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var queueName = receivePayload.ReadNextString();

                return new UnsubscribeQueue
                {
                    Id = id,
                    QueueName = queueName
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public QueueDeclare ToQueueDeclareModel(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var queueName = receivePayload.ReadNextString();
                var route = receivePayload.ReadNextString();

                return new QueueDeclare
                {
                    Id = id,
                    Name = queueName,
                    Route = route
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public QueueDelete ToQueueDeleteModel(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var queueName = receivePayload.ReadNextString();

                return new QueueDelete
                {
                    Id = id,
                    Name = queueName
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public ConfigureSubscription ToConfigureSubscription(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var concurrency = receivePayload.ReadNextInt();
                var boolInt = receivePayload.ReadNextInt();

                return new ConfigureSubscription
                {
                    Id = id,
                    Concurrency = concurrency,
                    AutoAck = boolInt == 1
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
        }

        public Ready ToReady(Memory<byte> _)
        {
            return new Ready();
        }

        #endregion

    }
}