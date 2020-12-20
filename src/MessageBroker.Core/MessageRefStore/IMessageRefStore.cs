using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.MessageRefStore
{
    public interface IMessageRefStore
    {
        void SetUpRefCounter(Guid messageId, int count);

        bool ReleaseOne(Guid messageId);
    }
}
