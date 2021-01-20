using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Models;
using MessageBroker.Client.SocketClient;
using MessageBroker.Models.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Client
{
    public class QueueConsumer
    {
        private readonly ISocketClient _socketClient;
        private readonly string _queueName;
        private readonly ISerializer _serializer;
        public event Action<QueueConsumerMessage> MessageReceived;
        public AutoResetEvent _inProgressResetEvent;
        
        public QueueConsumer(string queueName, ISerializer serializer, ISocketClient socketClient)
        {
            _queueName = queueName;
            _serializer = serializer;
            _socketClient = socketClient;
            _inProgressResetEvent = new(true);
        }

        public async Task SubscribeQueue()
        {
            _inProgressResetEvent.WaitOne();

            var sendPayload = SubscribeSendPayload();

            var result = await _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, true);

            _inProgressResetEvent.Set();
            
            if (!result)
                throw new InvalidOperationException();
        }

        public async Task UnSubscribeQueue()
        {
            _inProgressResetEvent.WaitOne();

            var sendPayload = UnSubscribeSendPayload();

            var result = await _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, true);

            _inProgressResetEvent.Set();
            
            if (!result)
                throw new InvalidOperationException();
        }

        private SendPayload SubscribeSendPayload()
        {
            var payload = new SubscribeQueue
            {
                Id = Guid.NewGuid(),
                QueueName = _queueName
            };

            return _serializer.ToSendPayload(payload);
        }
        
        private SendPayload UnSubscribeSendPayload()
        {
            var payload = new UnSubscribeQueue
            {
                Id = Guid.NewGuid(),
                QueueName = _queueName
            };

            return _serializer.ToSendPayload(payload);
        }
    }
}