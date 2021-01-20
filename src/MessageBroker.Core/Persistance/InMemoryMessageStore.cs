using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MessageBroker.Core.Persistance
{
    public class InMemoryMessageStore : IMessageStore
    {
        private readonly ConcurrentDictionary<Guid, byte[]> _messages;

        public InMemoryMessageStore()
        {
            _messages = new ConcurrentDictionary<Guid, byte[]>();
        }

        public ValueTask DeleteAsync(Guid messageId)
        {
            _messages.TryRemove(messageId, out _);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<byte[]> GetMessagesAsync()
        {
            foreach (var message in _messages)
            {
                await Task.Yield();
                yield return message.Value;
            }
        }

        public ValueTask InsertAsync(Guid messageId, byte[] message)
        {
            _messages[messageId] = message;
            return ValueTask.CompletedTask;
        }

        public ValueTask SetupAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}