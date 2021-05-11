// using System;
// using MessageBroker.Core;
// using MessageBroker.Core.Persistence.Messages;
// using MessageBroker.Core.Persistence.Messages.InMemoryStore;
// using MessageBroker.Core.Persistence.Redis;
// using MessageBroker.Core.Persistence.Topics;
// using MessageBroker.Core.Queues;
// using MessageBroker.Core.Queues.Store;
// using MessageBroker.Core.RouteMatching;
// using MessageBroker.Core.SessionPolicy;
// using MessageBroker.Core.Topics;
// using MessageBroker.Serialization;
// using Microsoft.Extensions.DependencyInjection;
// using Tests.Classes;
// using Xunit;
//
// namespace Tests.Core.Persistence.Queues
// {
//     public class RedisQueueStoreTests
//     {
//         [Fact]
//         public void MakeSureAddedQueueIsStoredAndWhenTryGetValueIsCalledTheQueueDataIsTheSameAndWhenQueueIsDeletedItCannotBeAccessedAnymore()
//         {
//             var redisConnectionProvider = new RedisConnectionProvider("localhost");
//             
//             var serviceCollection = new ServiceCollection();
//
//             serviceCollection.AddSingleton<IDispatchPolicy, DefaultDispatchPolicy>();
//             serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
//             serviceCollection.AddSingleton<IClientStore, ClientStore>();
//             serviceCollection.AddSingleton<IRouteMatcher, RouteMatcher>();
//             serviceCollection.AddSingleton<ISerializer, Serializer>();
//             serviceCollection.AddTransient<ITopic, Topic>();
//             
//             var redisQueueStore = new RedisTopicStore(redisConnectionProvider, serviceCollection.BuildServiceProvider());
//
//             var queueName = RandomGenerator.GenerateString(10);
//             var queueRoute = RandomGenerator.GenerateString(10);
//             
//             Assert.False(redisQueueStore.TryGetValue(queueName, out _));
//             
//             redisQueueStore.Add(queueName, queueRoute);
//             
//             Assert.True(redisQueueStore.TryGetValue(queueName, out var queue));
//             
//             Assert.Equal(queueName, queue.Name);
//             Assert.Equal(queueRoute, queue.Route);
//             
//             redisQueueStore.Delete(queueName);
//             
//             Assert.False(redisQueueStore.TryGetValue(queueName, out _));
//         }
//     }
// }