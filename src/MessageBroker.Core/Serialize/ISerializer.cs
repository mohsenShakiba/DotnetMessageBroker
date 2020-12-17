using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Serialize
{
    public interface ISerializer
    {
        byte[] Serialize(object o);
        object Deserialize(Memory<byte> b);

    }
}
