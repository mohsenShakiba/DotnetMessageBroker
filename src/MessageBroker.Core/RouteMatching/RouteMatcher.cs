using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]

namespace MessageBroker.Core.RouteMatching
{
    /// <inheritdoc />
    internal class RouteMatcher : IRouteMatcher
    {
        public bool Match(string messageRoute, string topicRoute)
        {
            if (messageRoute is null || topicRoute is null) return false;

            const string wildCard = "*";

            var messageRouteSegments = messageRoute.Split('/');
            var queueRouteSegments = topicRoute.Split('/');

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