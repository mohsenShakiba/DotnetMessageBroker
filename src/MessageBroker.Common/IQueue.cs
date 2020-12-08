using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Common
{
    public interface IQueue<T>
    {
        void Push(T item);
        bool TryPop(out T item);
    }
}
