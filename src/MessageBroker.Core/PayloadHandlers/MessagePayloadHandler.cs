using MessageBroker.Core.Persistence.Queues;
using MessageBroker.Models;

namespace MessageBroker.Core.PayloadHandlers
{
    public class MessagePayloadHandler
    {
        private readonly IQueueStore _queueStore;

        public MessagePayloadHandler(IQueueStore queueStore)
        {
            _queueStore = queueStore;
        }


        public void Process(Message message)
        {
            var didMatchAnyQueue = false;

            foreach (var queue in _queueStore.GetAll())
            {
                if (queue.MessageRouteMatch(message.Route))
                {
                    didMatchAnyQueue = true;
                    queue.OnMessage(message);
                }
                
            }

            if (didMatchAnyQueue)
            {
                // send ok
            }
            else
            {
                // send error
            }
            
        }
    }
}