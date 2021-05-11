using System;
using System.Threading.Tasks;
using MessageBroker.Client.ConnectionManagement;
using MessageBroker.Client.Payloads;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.ReceiveDataProcessing;
using MessageBroker.Client.SendDataProcessing;
using MessageBroker.Client.Subscriptions;
using MessageBroker.Client.TaskManager;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Pooling;
using MessageBroker.Core.Clients;
using MessageBroker.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBroker.Client
{
    public class BrokerClientFactory : IAsyncDisposable
    {
        
        private IServiceProvider _serviceProvider;

        public IBrokerClient GetClient()
        {
            SetupServiceProvider();

            var scope = _serviceProvider.CreateScope();
            
            return scope.ServiceProvider.GetRequiredService<IBrokerClient>();
        }

        private void SetupServiceProvider()
        {
            if (_serviceProvider is null)
            {
                var serviceCollection = new ServiceCollection();
                
                serviceCollection.AddSingleton<ISerializer, Serializer>();
                serviceCollection.AddSingleton<IDeserializer, Deserializer>();
                serviceCollection.AddSingleton<ISendPayloadTaskManager, SendPayloadTaskManager>();
                serviceCollection.AddSingleton<IPayloadFactory, PayloadFactory>();
                serviceCollection.AddSingleton<StringPool>();
                
                serviceCollection.AddScoped<IBinaryDataProcessor, BinaryDataProcessor>();
                serviceCollection.AddScoped<ISubscription, Subscription>();
                serviceCollection.AddScoped<IConnectionManager, ConnectionManager>();
                serviceCollection.AddScoped<ISendDataProcessor, SendDataProcessor>();
                serviceCollection.AddScoped<IReceiveDataProcessor, ReceiveDataProcessor>();
                serviceCollection.AddScoped<ISubscriptionStore, SubscriptionStore>();
                serviceCollection.AddScoped<IClient, Core.Clients.Client>();
                serviceCollection.AddScoped<IBrokerClient, BrokerClient>();

                _serviceProvider = serviceCollection.BuildServiceProvider();
            }
        }

        public ValueTask DisposeAsync()
        {
            if (_serviceProvider is not null)
            {
                var client = _serviceProvider.GetRequiredService<IBrokerClient>();
                return client.DisposeAsync();
            }
            
            return ValueTask.CompletedTask;
        }
    }
}