using System.Threading.Channels;
using MessageBroker.Models;

namespace MessageBroker.Client.Subscription
{
    public class QueueSubscription
    {
        public string QueueName { get; set; }
        public string Route { get; set; }
        
        public ChannelReader<QueueMessage> MessageChannel { get; }
    }
}