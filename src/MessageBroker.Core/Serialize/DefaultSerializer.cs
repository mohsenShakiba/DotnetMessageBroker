using MessageBroker.Core.BufferPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageBroker.Core.Payloads;

namespace MessageBroker.Core.Serialize
{
    public class DefaultSerializer: ISerializer
    {

        public PayloadType ParsePayloadType(Memory<byte> b)
        {
            var messageType = (PayloadType)BitConverter.ToInt32(b.Slice(0, 4).Span);
            return messageType;
        }

        #region Serialize


        public SendPayload ToSendPayload(Ack ack)
        {
            var sendPayload = ObjectPool.Shared.RentBinarySerializeHelper();

            return sendPayload
                .WriteType(PayloadType.Ack)
                .WriteId(ack.Id)
                .Build();
        }

        public SendPayload ToSendPayload(Message msg)
        {
            var sendPayload = ObjectPool.Shared.RentBinarySerializeHelper();

            return sendPayload
                .WriteType(PayloadType.Msg)
                .WriteId(msg.Id)
                .WriteStr(msg.Route)
                .WriteMemory(msg.Data)
                .Build();
        }

        public SendPayload ToSendPayload(Register register)
        {
            var sendPayload = ObjectPool.Shared.RentBinarySerializeHelper();

            return sendPayload
                .WriteType(PayloadType.Register)
                .WriteId(register.Id)
                .WriteInt(register.Concurrency)
                .Build();
        }

        public SendPayload ToSendPayload(SubscribeQueue subscribeQueue)
        {
            var sendPayload = ObjectPool.Shared.RentBinarySerializeHelper();

            return sendPayload
                .WriteType(PayloadType.SubscribeQueue)
                .WriteId(subscribeQueue.Id)
                .WriteStr(subscribeQueue.QueueName)
                .Build();
        }

        public SendPayload ToSendPayload(QueueDeclare queue)
        {
            var sendPayload = ObjectPool.Shared.RentBinarySerializeHelper();

            return sendPayload
                .WriteType(PayloadType.QueueCreate)
                .WriteId(queue.Id)
                .WriteStr(queue.Name)
                .WriteStr(queue.Route)
                .Build();
        }

        public SendPayload ToSendPayload(QueueDelete queue)
        {
            var sendPayload = ObjectPool.Shared.RentBinarySerializeHelper();

            return sendPayload
                .WriteType(PayloadType.QueueDelete)
                .WriteId(queue.Id)
                .WriteStr(queue.Name)
                .Build();
        }

        
        #endregion

        #region Deserialize

        public Ack ToAck(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.RentDeSerializeBinaryHelper();
            receivePayload.Setup(data);

            try
            {
                var messageId = receivePayload.ReadNextGuid();
                
                return new Ack { Id = messageId };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }

        }


        public Message ToMessage(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.RentDeSerializeBinaryHelper();
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

        public SubscribeQueue ToListenRoute(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.RentDeSerializeBinaryHelper();
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


        public Register ToSubscribe(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.RentDeSerializeBinaryHelper();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var concurrency = receivePayload.ReadNextInt();

                return new Register
                {
                    Id = id,
                    Concurrency = concurrency
                };
            }
            finally
            {
                ObjectPool.Shared.Return(receivePayload);
            }
            
            
        }

        public QueueDeclare ToQueueDeclareModel(Memory<byte> data)
        {
            var receivePayload = ObjectPool.Shared.RentDeSerializeBinaryHelper();
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
            var receivePayload = ObjectPool.Shared.RentDeSerializeBinaryHelper();
            receivePayload.Setup(data);

            try
            {
                var id = receivePayload.ReadNextGuid();
                var queueName = receivePayload.ReadNextString();

                return new QueueDelete
                {
                    Id = id,
                    Name = queueName,
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
