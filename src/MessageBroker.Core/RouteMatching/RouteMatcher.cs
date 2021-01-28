namespace MessageBroker.Core.RouteMatching
{
    public class RouteMatcher : IRouteMatcher
    {
        public bool Match(string messageRoute, string subscriberRoute)
        {
            // todo: a more complex routing 
            return messageRoute == subscriberRoute;
        }
    }
}