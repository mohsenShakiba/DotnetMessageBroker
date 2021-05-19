using StackExchange.Redis;

namespace MessageBroker.Core.Persistence.Redis
{
    /// <summary>
    /// Provides the endpoint for connecting to Redis server
    /// </summary>
    public class RedisConnectionProvider
    {
        private readonly string _urlConnection;
        private ConnectionMultiplexer _connection;

        /// <summary>
        /// Instantiates a new <see cref="RedisConnectionProvider" />
        /// </summary>
        /// <param name="urlConnection">Url connection of Redis server</param>
        public RedisConnectionProvider(string urlConnection)
        {
            _urlConnection = urlConnection;
        }

        /// <summary>
        /// Returns <see cref="ConnectionMultiplexer" /> that contains the Url connection of Redis server
        /// </summary>
        /// <returns><see cref="ConnectionMultiplexer" /> of Redis server</returns>
        public ConnectionMultiplexer Get()
        {
            return _connection ??= ConnectionMultiplexer.Connect(_urlConnection);
        }
    }
}