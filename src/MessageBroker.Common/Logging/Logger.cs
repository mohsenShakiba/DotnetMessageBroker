using Microsoft.Extensions.Logging;

namespace MessageBroker.Common.Logging
{
    public class Logger
    {
        private static ILogger<Logger> _defaultLogger;

        static Logger()
        {
            var loggerFactory = new LoggerFactory();
            _defaultLogger = loggerFactory.CreateLogger<Logger>();
        }

        public static void AddProvider(ILoggerProvider loggerProvider)
        {
            var loggerFactory = LoggerFactory.Create(b => { b.AddProvider(loggerProvider); });

            _defaultLogger = loggerFactory.CreateLogger<Logger>();
        } 

        public static void AddConsole()
        {
            var loggerFactory = LoggerFactory.Create(b => { b.AddConsole(); });

            _defaultLogger = loggerFactory.CreateLogger<Logger>();
        }

        public static void AddFile(string path)
        {
            var loggerFactory = LoggerFactory.Create(b => { b.AddFile(path); });
            _defaultLogger = loggerFactory.CreateLogger<Logger>();
        }
        
        public static void LogInformation(string template, params object[] arguments)
        {
            _defaultLogger.LogInformation(template, arguments);
        }

        public static void LogWarning(string template, params object[] arguments)
        {
            _defaultLogger.LogWarning(template, arguments);
        }

        public static void LogError(string template, params object[] arguments)
        {
            _defaultLogger.LogError(template, arguments);
        }
    }
}