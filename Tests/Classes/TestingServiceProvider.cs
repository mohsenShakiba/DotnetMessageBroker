// using MessageBroker.Core;
// using MessageBroker.Core.Broker;
// using MessageBroker.Serialization;
// using MessageBroker.TCP;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
//
// namespace Tests.Classes
// {
//     public static class TestingServiceProvider
//     {
//         public static IServiceCollection New()
//         {
//             var services = new ServiceCollection();
//
//             var loggerFactory = LoggerFactory.Create(_ => { });
//
//             services.AddSingleton<ISocketEventProcessor, Broker>();
//             services.AddSingleton<ISerializer, Serializer>();
//             services.AddSingleton(_ => loggerFactory);
//
//             return services;
//         }
//     }
// }