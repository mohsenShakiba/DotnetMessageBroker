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