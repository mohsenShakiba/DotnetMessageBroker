using System;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using MessageBroker.Models;
using MessageBroker.TCP.Binary;

namespace MessageBroker.Serialization
{
    /// <inheritdoc />
    public class Serializer : ISerializer
    {

        public SerializedPayload Serialize(Message msg)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();

            try
            {
                return binaryWriter
                    .WriteType(PayloadType.Msg)
                    .WriteId(msg.Id)
                    .WriteStr(msg.Route)
                    .WriteMemory(msg.Data)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(TopicMessage msg)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();

            try
            {
                return binaryWriter
                    .WriteType(PayloadType.TopicMessage)
                    .WriteId(msg.Id)
                    .WriteStr(msg.Route)
                    .WriteStr(msg.TopicName)
                    .WriteMemory(msg.Data)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(Ack ack)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();
            try
            {
                return binaryWriter
                    .WriteType(PayloadType.Ack)
                    .WriteId(ack.Id)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(Nack ack)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();
            try
            {
                return binaryWriter
                    .WriteType(PayloadType.Nack)
                    .WriteId(ack.Id)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(Ok ok)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();

            try
            {
                return binaryWriter
                    .WriteType(PayloadType.Ok)
                    .WriteId(ok.Id)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(Error error)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();
            try
            {
                return binaryWriter
                    .WriteType(PayloadType.Error)
                    .WriteId(error.Id)
                    .WriteStr(error.Message)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(SubscribeTopic subscribeTopic)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();
            try
            {
                return binaryWriter
                    .WriteType(PayloadType.SubscribeTopic)
                    .WriteId(subscribeTopic.Id)
                    .WriteStr(subscribeTopic.TopicName)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(UnsubscribeTopic unSubscribeTopic)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();
            try
            {
                return binaryWriter
                    .WriteType(PayloadType.UnsubscribeTopic)
                    .WriteId(unSubscribeTopic.Id)
                    .WriteStr(unSubscribeTopic.TopicName)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(TopicDeclare topicDeclare)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();

            try
            {
                return binaryWriter
                    .WriteType(PayloadType.TopicDeclare)
                    .WriteId(topicDeclare.Id)
                    .WriteStr(topicDeclare.Name)
                    .WriteStr(topicDeclare.Route)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }

        public SerializedPayload Serialize(TopicDelete topicDelete)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();

            try
            {
                return binaryWriter
                    .WriteType(PayloadType.TopicDelete)
                    .WriteId(topicDelete.Id)
                    .WriteStr(topicDelete.Name)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }


        public SerializedPayload Serialize(ConfigureClient configureClient)
        {
            var binaryWriter = ObjectPool.Shared.Rent<BinaryProtocolWriter>();
            
            try
            {
                return binaryWriter
                    .WriteType(PayloadType.Configure)
                    .WriteId(configureClient.Id)
                    .WriteInt(configureClient.PrefetchCount)
                    .ToSerializedPayload();
            }
            finally
            {
                ObjectPool.Shared.Return(binaryWriter);
            }
        }
    }
}