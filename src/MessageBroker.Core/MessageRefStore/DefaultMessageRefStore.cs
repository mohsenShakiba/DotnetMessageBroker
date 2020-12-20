using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Core.MessageRefStore
{
    public class DefaultMessageRefStore : IMessageRefStore
    {

        private readonly ConcurrentDictionary<Guid, int> _messageRefCountMap;

        public DefaultMessageRefStore()
        {
            _messageRefCountMap = new();
        }

        public bool ReleaseOne(Guid messageId)
        {
            if (_messageRefCountMap.TryGetValue(messageId, out var count))
            {
                if (count == 1)
                {
                    _messageRefCountMap.TryRemove(messageId, out _);
                    return true;
                }
                else
                {
                    while (true)
                    {
                        if (_messageRefCountMap.TryUpdate(messageId, count--, count))
                        {
                            break;
                        }
                    }
                }
            }

            return false;
        }

        public void SetUpRefCounter(Guid messageId, int count)
        {
            _messageRefCountMap[messageId] = count;
        }
    }
}
