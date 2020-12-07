using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Abstractions
{
    public interface IWritable
    {
        Task WriteAsync();
    }
}
