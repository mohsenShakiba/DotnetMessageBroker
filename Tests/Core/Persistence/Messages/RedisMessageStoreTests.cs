using System;
using System.Linq;
using System.Text;
using MessageBroker.Core.Persistence.Messages.RedisStore;
using MessageBroker.Core.Persistence.Redis;
using MessageBroker.Models;
using MessageBroker.Serialization;
using Tests.Classes;
using Xunit;

namespace Tests.Core.Persistence.Messages
{
    public class RedisMessageStoreTests
    {

        [Fact]
        public void MakeSureAddedMessageIsStoredAndWhenTryGetValueIsCalledTheMessageDataIsTheSame()
        {
            var redisConnectionProvider = new RedisConnectionProvider("localhost");
            var serializer = new Serializer();

            var redisMessageStore = new RedisMessageStore(redisConnectionProvider, serializer);

            var sampleMessage = new QueueMessage
            {
                Id = Guid.NewGuid(),
                Route = RandomGenerator.GenerateString(10),
                QueueName = RandomGenerator.GenerateString(10),
                Data = RandomGenerator.GenerateBytes(100)
            };
            
            Assert.False(redisMessageStore.TryGetValue(sampleMessage.Id, out _));
            
            redisMessageStore.Add(sampleMessage);
            
            Assert.True(redisMessageStore.TryGetValue(sampleMessage.Id, out var storedSampleMessage));
            
            Assert.Equal(sampleMessage.Id, storedSampleMessage.Id);
            Assert.Equal(sampleMessage.Route, storedSampleMessage.Route);
            Assert.Equal(sampleMessage.QueueName, storedSampleMessage.QueueName);
            Assert.Equal(Encoding.UTF8.GetString(sampleMessage.Data.Span), Encoding.UTF8.GetString(storedSampleMessage.Data.Span));
        }
        
        [Fact]
        public void MakeSureDeletedMessageCannotBeAccessedAnymore()
        {
            var redisConnectionProvider = new RedisConnectionProvider("localhost");
            var serializer = new Serializer();

            var redisMessageStore = new RedisMessageStore(redisConnectionProvider, serializer);

            var sampleMessage = new QueueMessage
            {
                Id = Guid.NewGuid(),
                Route = RandomGenerator.GenerateString(10),
                QueueName = RandomGenerator.GenerateString(10),
                Data = RandomGenerator.GenerateBytes(100)
            };
            
            redisMessageStore.Add(sampleMessage);
            redisMessageStore.Delete(sampleMessage.Id);
            
            Assert.False(redisMessageStore.TryGetValue(sampleMessage.Id, out var storedSampleMessage));
        }

        [Fact]
        public void MakeSureAddedMessagesCanBeRetrievedFromPendingMessages()
        {
            var redisConnectionProvider = new RedisConnectionProvider("localhost");
            var serializer = new Serializer();

            var redisMessageStore = new RedisMessageStore(redisConnectionProvider, serializer);

            var sampleMessage = new QueueMessage
            {
                Id = Guid.NewGuid(),
                Route = RandomGenerator.GenerateString(10),
                QueueName = RandomGenerator.GenerateString(10),
                Data = RandomGenerator.GenerateBytes(100)
            };
            
            redisMessageStore.Add(sampleMessage);

            var pendingMessages = redisMessageStore.PendingMessages(int.MaxValue);
            
            Assert.Contains(sampleMessage.Id, pendingMessages);
        }
    }
}