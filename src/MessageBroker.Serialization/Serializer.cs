using System;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.Serialization.Pools;

namespace MessageBroker.Serialization
{
    public class Serializer : ISerializer
    {

        public PayloadType ParsePayloadType(Memory<byte> b)
        {
            var messageType = (PayloadType) BitConverter.ToInt32(b.Slice(0, 4).Span);
            return messageType;
        }

        #region Serialize

        public SendPayload ToSendPayload(Ack ack)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Ack)
                .WriteId(ack.Id)
                .Build();
        }

        public SendPayload ToSendPayload(Nack ack)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();
            
            return sendPayload
                .WriteType(PayloadType.Nack)
                .WriteId(ack.Id)
                .Build();
        }

        public SendPayload ToSendPayload(Message msg)
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

        public SendPayload ToSendPayload(SubscribeQueue subscribeQueue)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.SubscribeQueue)
                .WriteId(subscribeQueue.Id)
                .WriteStr(subscribeQueue.QueueName)
                .Build();
        }

        public SendPayload ToSendPayload(UnsubscribeQueue subscribeQueue)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.UnSubscribeQueue)
                .WriteId(subscribeQueue.Id)
                .WriteStr(subscribeQueue.QueueName)
                .Build();
        }

        public SendPayload ToSendPayload(QueueDeclare queueDeclare)
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

        public SendPayload ToSendPayload(QueueDelete queueDelete)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.QueueDelete)
                .WriteId(queueDelete.Id)
                .WriteStr(queueDelete.Name)
                .Build();
        }
        
        public SendPayload ToSendPayload(ConfigureSubscription configureSubscription)
        {
            var sendPayload = ObjectPool.Shared.Rent<BinarySerializeHelper>();
            sendPayload.Setup();

            return sendPayload
                .WriteType(PayloadType.Register)
                .WriteId(configureSubscription.Id)
                .WriteInt(configureSubscription.Concurrency)
                .WriteInt(configureSubscription.AutoAck ? 1 : 0)
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


        public Message ToMessage(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.Rent<BinaryDeserializeHelper>();
            receivePayload.Setup(data);

            try
            {
                var messageId = receivePayload.ReadNextGuid();
                var route = receivePayload.ReadNextString();
                var messageMemoryOwner = receivePayload.ReadNextBytes();

                return new Message
                {
                    Id = messageId,
                    Route = route,
                    Data = messageMemoryOwner,
                    OriginalMessageData = messageMemoryOwner
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

                return new UnsubscribeQueue()
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

                return new ConfigureSubscription()
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

        #endregion
    }
}