using System;

namespace MessageBroker.Core.Clients.Store
{
    /// <summary>
    /// A repository for storing and retrieving <see cref="IClient" /> objects
    /// </summary>
    public interface IClientStore
    {
        /// <summary>
        /// When a new <see cref="IClient" /> has been added
        /// </summary>
        /// <param name="client">The client object</param>
        void Add(IClient client);

        /// <summary>
        /// When a <see cref="IClient" /> has been removed
        /// </summary>
        /// <param name="client">The client object</param>
        void Remove(IClient client);

        /// <summary>
        /// Try to get an object of type <see cref="IClient" />
        /// </summary>
        /// <param name="clientId">Identifier of the <see cref="IClient" /></param>
        /// <param name="client">The object of type <see cref="IClient" /></param>
        /// <returns>Returns true if object is found</returns>
        bool TryGet(Guid clientId, out IClient client);
    }
}