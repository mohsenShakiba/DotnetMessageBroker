namespace MessageBroker.Core.RouteMatching
{
    /// <summary>
    /// Object for matching routes for messages and topics
    /// </summary>
    public interface IRouteMatcher
    {
        /// <summary>
        /// Will check if the route of message can be matched against route of topic
        /// </summary>
        /// <param name="messageRoute">Route of message</param>
        /// <param name="topicRoute">Route of topic</param>
        /// <returns>True if routes can be matched</returns>
        bool Match(string messageRoute, string topicRoute);
    }
}