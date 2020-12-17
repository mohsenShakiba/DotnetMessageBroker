using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Core.RouteMatching
{
    public interface IRouteMatcher
    {
        bool Match(string messageRoute, string subscriberRoute);
    }
}
