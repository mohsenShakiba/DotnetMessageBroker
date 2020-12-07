using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Core.RouteMatching
{
    public class DefaultRouteMatching : IRouteMatcher
    {
        public bool Match(string messageRoute, string subscriberRoute)
        {
            return messageRoute == subscriberRoute;
        }
    }
}
