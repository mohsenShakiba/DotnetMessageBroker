using StackExchange.Redis;

namespace MessageBroker.Core.Persistence.Redis
{
    public interface IRedisConnectionProvider
    {
        ConnectionMultiplexer Get();
    }
}