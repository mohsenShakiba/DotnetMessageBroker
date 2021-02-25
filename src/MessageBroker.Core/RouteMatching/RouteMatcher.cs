using System;

namespace MessageBroker.Core.RouteMatching
{
    public class RouteMatcher : IRouteMatcher
    {
        public bool Match(string messageRoute, string queueRoute)
        {
            const string wildCard = "*";

            var messageRouteSegments = messageRoute.Split('/');
            var queueRouteSegments = queueRoute.Split('/');

            var minSegmentCount = Math.Min(messageRouteSegments.Length, queueRouteSegments.Length);

            for (var i = 0; i < minSegmentCount; i++)
            {
                var messageSegment = messageRouteSegments[i];
                var queueSegment = queueRouteSegments[i];

                if (messageSegment == wildCard || queueSegment == wildCard)
                    continue;

                if (messageSegment == queueSegment)
                    continue;

                return false;
            }

            return true;
        }
    }
}