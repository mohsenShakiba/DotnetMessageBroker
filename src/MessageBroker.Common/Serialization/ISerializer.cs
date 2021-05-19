using MessageBroker.Common.Binary;
using MessageBroker.Common.Models;

namespace MessageBroker.Common.Serialization
{
    /// <summary>
    /// Serializes a payload to <see cref="SerializedPayload" />
    /// </summary>
    public interface ISerializer
    {
        SerializedPayload Serialize(Ok ok);
        SerializedPayload Serialize(Ack ack);
        SerializedPayload Serialize(Nack nack);
        SerializedPayload Serialize(Message msg);
        SerializedPayload Serialize(TopicMessage msg);
        SerializedPayload Serialize(SubscribeTopic subscribeTopic);
        SerializedPayload Serialize(UnsubscribeTopic unSubscribeTopic);
        SerializedPayload Serialize(TopicDeclare topicDeclare);
        SerializedPayload Serialize(TopicDelete topicDelete);
        SerializedPayload Serialize(Error error);
        SerializedPayload Serialize(ConfigureClient configureClient);
    }
}