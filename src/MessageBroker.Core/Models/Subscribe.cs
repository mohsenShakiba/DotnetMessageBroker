using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    public record Subscribe(Guid Id, int Concurrency): IPayload;
}
