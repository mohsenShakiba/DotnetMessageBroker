using MessageBroker.Core.Exceptions;
using MessageBroker.Core.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Persistance
{

    public class RedisConfiguration
    {
        public string ConnectionUrl { get; private set; }
        public string KeyPrefix { get; private set; }

        public RedisConfiguration(string connectionUrl, string keyPrefix)
        {
            ConnectionUrl = connectionUrl;
            KeyPrefix = keyPrefix;
        }
    }


    public class RedisMessageStore : IMessageStore
    {
        private readonly RedisConfiguration _config;
        private readonly List<(Guid MessageId, byte[] Data)> _addedMessages;
        private readonly List<Guid> _deletedMessages;

        private ConnectionMultiplexer _connection;
        private bool _isConnected;

        public RedisMessageStore(RedisConfiguration config)
        {
            _addedMessages = new();
            _deletedMessages = new();
            _config = config;
        }

        public async ValueTask DeleteAsync(Guid messageId)
        {
            if (!_isConnected)
            {
                _deletedMessages.Add(messageId);
                return;
            }

            var key = GetKeyForMessageId(messageId);
            await _connection.GetDatabase().KeyDeleteAsync(key);
        }

        public async IAsyncEnumerable<byte[]> GetMessagesAsync()
        {
            if (!_isConnected)
            {
                throw new StoreNotReadyException();
            }

            var keys = _connection.GetServer(_config.ConnectionUrl).KeysAsync(pattern: _config.KeyPrefix + ".*");

            await foreach (var key in keys)
            {
                yield return _connection.GetDatabase().StringGet(key);
            }
        }

        public async ValueTask InsertAsync(Guid messageId, byte[] data)
        {
            if (!_isConnected)
            {
                _addedMessages.Add((messageId, data));
            }

            var key = GetKeyForMessageId(messageId);
            await _connection.GetDatabase().StringSetAsync(key, data);
        }

        public async ValueTask SetupAsync()
        {
            _connection = await ConnectionMultiplexer.ConnectAsync(_config.ConnectionUrl);

            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ConnectionRestored += OnConnectionRestored;
        }

        private async void OnConnectionRestored(object _, ConnectionFailedEventArgs e)
        {
            _isConnected = true;

            foreach(var key in _deletedMessages)
            {
                await DeleteAsync(key);
            }

            foreach(var addedMessage in _addedMessages)
            {
                await InsertAsync(addedMessage.MessageId, addedMessage.Data);
            }
        }

        private void OnConnectionFailed(object _, ConnectionFailedEventArgs e)
        {
            _isConnected = false;
        }

        private RedisKey GetKeyForMessageId(Guid id)
        {
            return _config.KeyPrefix + "." + id.ToString();
        }

    }
}
