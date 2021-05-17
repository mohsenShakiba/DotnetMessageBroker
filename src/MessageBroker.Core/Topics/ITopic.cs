using System;
using System.Threading.Tasks;
using MessageBroker.Core.Clients;
using MessageBroker.Models;

namespace MessageBroker.Core.Topics
{
    
    /// <summary>
    /// Topic is an entity that has its own message queue, name and route
    /// messages sent to a matching route will end up in topic's queue and then sent
    /// to topic subscribers
    /// </summary>
    public interface ITopic: IDisposable
    {
        /// <summary>
        /// Name of topic, acts as an identifier
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Route of topic, used for dispatching messages
        /// messages with matching route will be enqueued by topic
        /// </summary>
        string Route { get; }
        
        /// <summary>
        /// Setup will set name and route of topic
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <param name="route">Route of topic</param>
        void Setup(string name, string route);

        /// <summary>
        /// Will continuously check queue for new messages to process
        /// </summary>
        void StartProcessingMessages();
        
        /// <summary>
        /// Read the next available message from topic's queue and process it
        /// </summary>
        /// <remarks>Used for testing purposes only</remarks>
        /// <returns>Task for when message is processed</returns>
        Task ReadNextMessage();
        
        /// <summary>
        /// Called by the IPayloadProcessor to add message to topic
        /// </summary>
        /// <param name="message">Message processed by IPayloadProcessor</param>
        void OnMessage(Message message);

        /// <summary>
        /// Called by IPayloadProcessor to add session to list of topic subscribers
        /// </summary>
        /// <param name="client">The <see cref="IClient"/> to be added </param>
        void ClientSubscribed(IClient client);
        
        /// <summary>
        /// Called by IPayloadProcessor to remove session from list of topic subscribers
        /// </summary>
        /// <param name="client">The <see cref="IClient"/> to be added </param>
        void ClientUnsubscribed(IClient client);
        
        /// <summary>
        /// Called by IPayloadProcessor to check if message route matches that of topic
        /// </summary>
        /// <param name="messageRoute">Route of message</param>
        /// <returns>True if message route matches</returns>
        bool MessageRouteMatch(string messageRoute);
    }
}