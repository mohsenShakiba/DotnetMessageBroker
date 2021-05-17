using System.Collections.Generic;
using MessageBroker.Core.Topics;

namespace MessageBroker.Core.Persistence.Topics
{
    /// <summary>
    /// repository for <see cref="ITopic"/>
    /// </summary>
    public interface ITopicStore
    {
        /// <summary>
        /// Called for initializing the store when store is created
        /// </summary>
        void Setup();
        
        /// <summary>
        /// Returns the complete list of topics
        /// </summary>
        /// <returns>List of topics</returns>
        IEnumerable<ITopic> GetAll();
        
        /// <summary>
        /// Add new topic to store
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <param name="route">Route of topic</param>
        void Add(string name, string route);
        
        /// <summary>
        /// Get topic by name
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <param name="topic">Topic itself</param>
        /// <returns></returns>
        bool TryGetValue(string name, out ITopic topic);
        
        /// <summary>
        /// Deletes a topic by name
        /// </summary>
        /// <param name="name">Name of topic</param>
        void Delete(string name);
    }
}