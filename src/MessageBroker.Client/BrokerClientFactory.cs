using System;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Payloads;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.SendDataProcessing;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Client.Subscriptions.Store;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using MessageBroker.Common.Serialization;
using MessageBroker.Core.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client
{
    public class BrokerClientFactory
    {
        public IBrokerClient GetClient()
        {
            var serviceProvider = SetupServiceProvider();
            
            return serviceProvider.GetRequiredService<IBrokerClient>();
        }

        private IServiceProvider SetupServiceProvider(Action<ServiceCollection> configure = default)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging();
            
            serviceCollection.AddSingleton<ISerializer, Serializer>();
            serviceCollection.AddSingleton<IDeserializer, Deserializer>();
            serviceCollection.AddSingleton<ITaskManager, TaskManager.TaskManager>();
            serviceCollection.AddSingleton<IPayloadFactory, PayloadFactory>();
            serviceCollection.AddSingleton<StringPool>();
            serviceCollection.AddSingleton<IConnectionManager, ConnectionManager>();
            serviceCollection.AddSingleton<ISendDataProcessor, SendDataProcessor>();
            serviceCollection.AddSingleton<IReceiveDataProcessor, ReceiveDataProcessor>();
            serviceCollection.AddSingleton<ISubscriptionStore, SubscriptionStore>();
            serviceCollection.AddSingleton<IBrokerClient, BrokerClient>();

            serviceCollection.AddTransient<IBinaryDataProcessor, BinaryDataProcessor>();
            serviceCollection.AddTransient<ISubscription, Subscription>();
            serviceCollection.AddTransient<IClient, Core.Clients.Client>();
            
            configure?.Invoke(serviceCollection);
            
            return serviceCollection.BuildServiceProvider();
        }
    }
}