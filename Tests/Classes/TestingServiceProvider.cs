using MessageBroker.Core;
using MessageBroker.Serialization;
using MessageBroker.SocketServer;
using MessageBroker.SocketServer.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests.Classes
{
    public static class TestingServiceProvider
    {
        public static IServiceCollection New()
        {
            var services = new ServiceCollection();

            var loggerFactory = LoggerFactory.Create(_ => { });
            var sessionConfiguration = SessionConfiguration.Default();

            services.AddSingleton<ISessionResolver, SessionResolver>();
            services.AddSingleton<ISessionEventListener, TcpSocketServer>();
            services.AddSingleton<ISocketEventProcessor, Coordinator>();
            services.AddSingleton<ISerializer, Serializer>();
            services.AddSingleton(_ => loggerFactory);
            services.AddSingleton(_ => sessionConfiguration);

            return services;
        }
    }
}