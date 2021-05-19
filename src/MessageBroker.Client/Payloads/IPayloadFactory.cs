using System;
using MessageBroker.Common.Binary;
using MessageBroker.Common.Models;

namespace MessageBroker.Client.Payloads
{
    /// <summary>
    /// Factory pattern for create <see cref="SerializedPayload" /> from payloads
    /// </summary>
    public interface IPayloadFactory
    {
        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="SubscribeTopic" /> payload
        /// </summary>
        /// <param name="topicName">Name of topic</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewSubscribeTopic(string topicName);

        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="UnsubscribeTopic" /> payload
        /// </summary>
        /// <param name="topicName">Name of topic</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewUnsubscribeTopic(string topicName);

        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="Ack" /> payload
        /// </summary>
        /// <param name="messageId">Identifier of the message</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewAck(Guid messageId);

        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="Nack" /> payload
        /// </summary>
        /// <param name="messageId">Identifier of the message</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewNack(Guid messageId);

        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="Message" /> payload
        /// </summary>
        /// <param name="data">Data to be sent in message</param>
        /// <param name="route">Route of the receiving topic</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewMessage(byte[] data, string route);

        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="TopicDeclare" /> payload
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <param name="route">Route of topic</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewDeclareTopic(string name, string route);

        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="TopicDelete" /> payload
        /// </summary>
        /// <param name="name">Name of topic</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewDeleteTopic(string name);

        /// <summary>
        /// Will create a new <see cref="SerializedPayload" /> of <see cref="ConfigureClient" /> payload
        /// </summary>
        /// <param name="prefetchCount">Number of prefetched count</param>
        /// <returns><see cref="SerializedPayload" /> containing the data</returns>
        SerializedPayload NewConfigureClient(int prefetchCount);
    }
}