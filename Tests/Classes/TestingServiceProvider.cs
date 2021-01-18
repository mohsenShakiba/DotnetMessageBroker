using System;
using MessageBroker.Core;
using MessageBroker.Core.Serialize;
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
            services.AddSingleton<ISerializer, DefaultSerializer>();
            services.AddSingleton(_ => loggerFactory);
            services.AddSingleton(_ => sessionConfiguration);

            return services;

        }
    }
}