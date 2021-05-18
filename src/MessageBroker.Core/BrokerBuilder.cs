using System;
using System.Net;
using MessageBroker.Common.Serialization;
using MessageBroker.Common.Tcp;
using MessageBroker.Core.Clients;
using MessageBroker.Core.Clients.Store;
using MessageBroker.Core.Dispatching;
using MessageBroker.Core.PayloadProcessing;
using MessageBroker.Core.Persistence.Messages;
using MessageBroker.Core.Persistence.Redis;
using MessageBroker.Core.Persistence.Topics;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Core
{
    /// <summary>
    /// Builder for creating an IBroker
    /// </summary>
    public class BrokerBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        public BrokerBuilder()
        {
            _serviceCollection = new ServiceCollection();
        }

        /// <summary>
        /// Specify the endpoint which socket server will listen on
        /// </summary>
        /// <param name="endPoint">Socket server endpoint</param>
        public BrokerBuilder UseEndPoint(IPEndPoint endPoint)
        {
            var connectionProvider = new ConnectionProvider {IpEndPoint = endPoint};
            _serviceCollection.AddSingleton(connectionProvider);
            return this;
        }

        /// <summary>
        /// Use memory based stores
        /// </summary>
        /// <remarks>Not recommended for production</remarks>
        public BrokerBuilder UseMemoryStore()
        {
            _serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
            _serviceCollection.AddSingleton<ITopicStore, InMemoryTopicStore>();
            return this;
        }

        /// <summary>
        /// Use redis based store
        /// </summary>
        /// <param name="connectionString">Connection string to redis server</param>
        public BrokerBuilder UserRedisStore(string connectionString)
        {
            var redisConnectionProvider = new RedisConnectionProvider(connectionString);

            _serviceCollection.AddSingleton(redisConnectionProvider);
            _serviceCollection.AddSingleton<IMessageStore, RedisMessageStore>();
            _serviceCollection.AddSingleton<ITopicStore, InMemoryTopicStore>();

            return this;
        }

        /// <summary>
        /// Configure logger
        /// </summary>
        /// <param name="loggerBuilder">Action for configuring logging</param>
        public BrokerBuilder ConfigureLogger(Action<ILoggingBuilder> loggerBuilder)
        {
            _serviceCollection.AddLogging(loggerBuilder);
            return this;
        }

        /// <summary>
        /// Add console log provider
        /// </summary>
        public BrokerBuilder AddConsoleLog()
        {
            ConfigureLogger(builder => { builder.AddConsole(); });

            return this;
        }

        /// <summary>
        /// Build IBroker
        /// </summary>
        /// <returns></returns>
        public IBroker Build()
        {
            AddRequiredServices();
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IBroker>();
        }

        private void AddRequiredServices()
        {
            _serviceCollection.AddLogging();
            
            _serviceCollection.AddSingleton<IPayloadProcessor, PayloadProcessor>();
            _serviceCollection.AddSingleton<IClientStore, ClientStore>();
            _serviceCollection.AddSingleton<IMessageStore, InMemoryMessageStore>();
            _serviceCollection.AddSingleton<ISerializer, Serializer>();
            _serviceCollection.AddSingleton<IDeserializer, Deserializer>();
            _serviceCollection.AddSingleton<IRouteMatcher, RouteMatcher>();
            _serviceCollection.AddSingleton<IListener, TcpListener>();
            _serviceCollection.AddSingleton<IBroker, Broker>();

            _serviceCollection.AddTransient<IClient, Client>();
            _serviceCollection.AddTransient<IDispatcher, DefaultDispatcher>();
            _serviceCollection.AddTransient<ITopic, Topic>();
        }
    }
}