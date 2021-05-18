using System;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Models;
using MessageBroker.Common.Pooling;

namespace MessageBroker.Common.Serialization
{
    /// <inheritdoc />
    public class Deserializer : IDeserializer
    {
        public PayloadType ParsePayloadType(Memory<byte> b)
        {
            var typeSlice = BitConverter.ToInt32(b.Span[..BinaryProtocolConfiguration.SizeForInt]);
            return (PayloadType) typeSlice;
        }

        public Ack ToAck(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var messageId = binaryReader.ReadNextGuid();
                return new Ack {Id = messageId};
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public Nack ToNack(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var messageId = binaryReader.ReadNextGuid();
                return new Nack {Id = messageId};
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public Ok ToOk(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var id = binaryReader.ReadNextGuid();

                return new Ok
                {
                    Id = id
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }


        public Error ToError(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var id = binaryReader.ReadNextGuid();
                var message = binaryReader.ReadNextString();

                return new Error
                {
                    Id = id,
                    Message = message
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }


        public Message ToMessage(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var messageId = binaryReader.ReadNextGuid();
                var route = binaryReader.ReadNextString();
                var dataSize = binaryReader.ReadNextBytes();

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
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public TopicMessage ToTopicMessage(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var messageId = binaryReader.ReadNextGuid();
                var route = binaryReader.ReadNextString();
                var queueName = binaryReader.ReadNextString();
                var dataSize = binaryReader.ReadNextBytes();

                return new TopicMessage
                {
                    Id = messageId,
                    Route = route,
                    TopicName = queueName,
                    Data = dataSize.OriginalData.AsMemory(0, dataSize.Size),
                    OriginalMessageData = dataSize.OriginalData
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public SubscribeTopic ToSubscribeTopic(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var id = binaryReader.ReadNextGuid();
                var queueName = binaryReader.ReadNextString();

                return new SubscribeTopic
                {
                    Id = id,
                    TopicName = queueName
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public UnsubscribeTopic ToUnsubscribeTopic(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var id = binaryReader.ReadNextGuid();
                var queueName = binaryReader.ReadNextString();

                return new UnsubscribeTopic
                {
                    Id = id,
                    TopicName = queueName
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public TopicDeclare ToTopicDeclare(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var id = binaryReader.ReadNextGuid();
                var queueName = binaryReader.ReadNextString();
                var route = binaryReader.ReadNextString();

                return new TopicDeclare
                {
                    Id = id,
                    Name = queueName,
                    Route = route
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public TopicDelete ToTopicDelete(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var id = binaryReader.ReadNextGuid();
                var queueName = binaryReader.ReadNextString();

                return new TopicDelete
                {
                    Id = id,
                    Name = queueName
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }

        public ConfigureClient ToConfigureClient(Memory<byte> data)
        {
            var binaryReader = ObjectPool.Shared.Rent<BinaryProtocolReader>();
            binaryReader.Setup(data);

            try
            {
                var id = binaryReader.ReadNextGuid();
                var prefetchCount = binaryReader.ReadNextInt();

                return new ConfigureClient
                {
                    Id = id,
                    PrefetchCount = prefetchCount
                };
            }
            finally
            {
                ObjectPool.Shared.Return(binaryReader);
            }
        }
    }
}