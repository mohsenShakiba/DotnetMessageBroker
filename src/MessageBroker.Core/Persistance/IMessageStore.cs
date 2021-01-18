using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Persistance
{
    public interface IMessageStore
    {
        ValueTask SetupAsync();
        ValueTask InsertAsync(Guid messageId, byte[] data);
        ValueTask DeleteAsync(Guid messageId);
        IAsyncEnumerable<byte[]> GetMessagesAsync();
    }
}
