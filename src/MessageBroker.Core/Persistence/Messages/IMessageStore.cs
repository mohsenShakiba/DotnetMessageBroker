using System;
using System.Collections.Generic;
using MessageBroker.Models;

namespace MessageBroker.Core.Persistence.Messages
{
    /// <summary>
    /// Repository for <see cref="TopicMessage"/>
    /// </summary>
    public interface IMessageStore
    {
        /// <summary>
        /// Called for initializing the store when store is created
        /// </summary>
        void Setup();
        
        /// <summary>
        /// Stores a <see cref="TopicMessage"/>
        /// </summary>
        /// <param name="message">Message to be stored</param>
        void Add(TopicMessage message);
        
        /// <summary>
        /// Try to get message by id
        /// </summary>
        /// <param name="id">Identifier of the message</param>
        /// <param name="message">Found message</param>
        /// <returns>If the message was found</returns>
        bool TryGetValue(Guid id, out TopicMessage message);
        
        /// <summary>
        /// Delete message by id
        /// </summary>
        /// <param name="id">Identifier of the message</param>
        void Delete(Guid id);
        
        /// <summary>
        /// Returns a list of all messages
        /// </summary>
        /// <returns>List of message</returns>
        /// <remarks>Called when topic is initialized to enqueue all the pending messages</remarks>
        IEnumerable<Guid> GetAll();
    }
}