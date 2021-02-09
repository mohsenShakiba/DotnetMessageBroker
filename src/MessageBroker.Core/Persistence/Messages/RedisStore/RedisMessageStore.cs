using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.Persistence.InMemoryStore;
using MessageBroker.Core.Persistence.Redis;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Core.Persistence.Messages.RedisStore
{
    public class RedisMessageStore: IMessageStore
    {
        private readonly IRedisConnectionProvider _redisConnectionProvider;
        private readonly ISerializer _serializer;
        private readonly InMemoryMessageStore _inMemoryMessageStore;
        private const string MessageRedisKey = "MessageBroker.Messages.Set";

        public RedisMessageStore(IRedisConnectionProvider redisConnectionProvider, ISerializer serializer)
        {
            _redisConnectionProvider = redisConnectionProvider;
            _inMemoryMessageStore = new();
            _serializer = serializer;
        }
        
        public void Setup()
        {
            var connection = _redisConnectionProvider.Get();
            var messages = connection.GetDatabase().SetScan(MessageRedisKey, int.MaxValue);
            foreach (var messageData in messages)
            {
                var message = Deserialize((byte[])messageData);
                _inMemoryMessageStore.InsertAsync(message);
            }
        }

        public void InsertAsync(QueueMessage message)
        {
            _inMemoryMessageStore.InsertAsync(message);
            var connection = _redisConnectionProvider.Get();
            var serializedMessage = Serialize(message);
            connection.GetDatabase().SetAdd(MessageRedisKey, serializedMessage);
        }

        public bool TryGetValue(Guid id, out QueueMessage message)
        {
            return _inMemoryMessageStore.TryGetValue(id, out message);
        }

        public void DeleteAsync(Guid id)
        {
            if (_inMemoryMessageStore.TryGetValue(id, out var message))
            {
                var connection = _redisConnectionProvider.Get();
                var serializedMessage = Serialize(message);
                connection.GetDatabase().SetRemove(MessageRedisKey, serializedMessage);

                _inMemoryMessageStore.DeleteAsync(id);
                message.Dispose();
            }
        }

        public IEnumerable<Guid> PendingMessages(int count)
        {
            return _inMemoryMessageStore.PendingMessages(count);
        }

        private QueueMessage Deserialize(Memory<byte> value)
        {
            return _serializer.ToQueueMessage(value);
        }

        private Memory<byte> Serialize(QueueMessage message)
        {
            var serializedData = _serializer.Serialize(message);
            return serializedData.DataWithoutSize;
        }
    }
}