using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Core
{
    public interface IRouteMatcher
    {
        bool Match(string messageRoute, string subscriberRoute);
    }
}
