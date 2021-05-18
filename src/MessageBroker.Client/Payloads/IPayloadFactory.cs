using System;
using MessageBroker.Common.Binary;

namespace MessageBroker.Client.Payloads
{
    /// <summary>
    /// Factory pattern for create <see cref="SerializedPayload" /> from payloads
    /// </summary>
    public interface IPayloadFactory
    {
        SerializedPayload NewSubscribeTopic(string topicName);
        SerializedPayload NewUnsubscribeTopic(string topicName);
        SerializedPayload NewAck(Guid messageId);
        SerializedPayload NewNack(Guid messageId);
        SerializedPayload NewMessage(byte[] data, string route);
        SerializedPayload NewDeclareTopic(string name, string route);
        SerializedPayload NewDeleteTopic(string name);
        SerializedPayload NewConfigureClient(int prefetchCount);
    }
}