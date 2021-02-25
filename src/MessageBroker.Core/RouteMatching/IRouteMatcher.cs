namespace MessageBroker.Core.RouteMatching
{
    public interface IRouteMatcher
    {
        bool Match(string messageRoute, string queueRoute);
    }
}