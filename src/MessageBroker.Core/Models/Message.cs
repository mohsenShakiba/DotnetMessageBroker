using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    public record Message(Guid Id, string Route, byte[] Data, int BufferExceedCount): IPayload;
}
