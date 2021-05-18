using StackExchange.Redis;

namespace MessageBroker.Core.Persistence.Redis
{
    public class RedisConnectionProvider
    {
        private readonly string _urlConnection;
        private ConnectionMultiplexer _connection;

        public RedisConnectionProvider(string urlConnection)
        {
            _urlConnection = urlConnection;
        }

        public ConnectionMultiplexer Get()
        {
            return _connection ??= ConnectionMultiplexer.Connect(_urlConnection);
        }
    }
}