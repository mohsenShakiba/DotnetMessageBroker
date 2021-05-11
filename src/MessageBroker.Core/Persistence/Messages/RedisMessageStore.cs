using System;
using System.Collections.Generic;
using MessageBroker.Core.Persistence.Redis;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Core.Persistence.Messages
{
    /// <inheritdoc />
    public class RedisMessageStore : IMessageStore
    {
        private const string MessageRedisKey = "MessageBroker.Messages.Set";
        private readonly InMemoryMessageStore _inMemoryMessageStore;
        private readonly RedisConnectionProvider _redisConnectionProvider;
        private readonly IDeserializer _deserializer;
        private readonly ISerializer _serializer;

        public RedisMessageStore(RedisConnectionProvider redisConnectionProvider, IDeserializer deserializer, ISerializer serializer)
        {
            _redisConnectionProvider = redisConnectionProvider;
            _inMemoryMessageStore = new InMemoryMessageStore();
            _deserializer = deserializer;
            _serializer = serializer;
        }

        public void Setup()
        {
            var connection = _redisConnectionProvider.Get();
            var messages = connection.GetDatabase().SetScan(MessageRedisKey, int.MaxValue);
            foreach (var messageData in messages)
            {
                var message = Deserialize((byte[]) messageData);
                _inMemoryMessageStore.Add(message);
            }
        }

        public void Add(TopicMessage message)
        {
            _inMemoryMessageStore.Add(message);
            var connection = _redisConnectionProvider.Get();
            var serializedMessage = Serialize(message);
            connection.GetDatabase().SetAdd(MessageRedisKey, serializedMessage);
        }

        public bool TryGetValue(Guid id, out TopicMessage message)
        {
            return _inMemoryMessageStore.TryGetValue(id, out message);
        }

        public void Delete(Guid id)
        {
            if (_inMemoryMessageStore.TryGetValue(id, out var message))
            {
                var connection = _redisConnectionProvider.Get();
                var serializedMessage = Serialize(message);
                connection.GetDatabase().SetRemove(MessageRedisKey, serializedMessage);

                _inMemoryMessageStore.Delete(id);
                message.Dispose();
            }
        }

        public IEnumerable<Guid> GetAll()
        {
            return _inMemoryMessageStore.GetAll();
        }

        private TopicMessage Deserialize(Memory<byte> value)
        {
            return _deserializer.ToTopicMessage(value);
        }

        private Memory<byte> Serialize(TopicMessage message)
        {
            var serializedData = _serializer.Serialize(message);
            return serializedData.DataWithoutSize;
        }
    }
}