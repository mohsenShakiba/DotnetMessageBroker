using MessageBroker.Core.Clients;
using MessageBroker.Core.Topics;

namespace MessageBroker.Core.DispatchPolicy
{
    /// <summary>
    /// Used by <see cref="ITopic"/> to select <see cref="IClient"/> from list of topic subscribers 
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Called when a new <see cref="IClient"/> has subscribed to a topic
        /// </summary>
        /// <param name="client">Client that subscribed</param>
        void Add(IClient client);

        /// <summary>
        /// Called when <see cref="IClient"/> has unsubscribed
        /// </summary>
        /// <param name="client">Client that unsubscribed</param>
        /// <returns>True if client is subscribed, false if client is not subscribed</returns>
        bool Remove(IClient client);
        
        /// <summary>
        /// Get the next available <see cref="IClient"/> from list of subscribed clients
        /// </summary>
        /// <returns>Next available <see cref="IClient"/> otherwise returns null</returns>
        IClient NextAvailable();
    }
}