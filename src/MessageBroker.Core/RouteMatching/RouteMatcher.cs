using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Core.RouteMatching
{
    public class RouteMatcher : IRouteMatcher
    {
        public bool Match(string messageRoute, string subscriberRoute)
        {
            return messageRoute == subscriberRoute;
        }
    }
}
