using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.Models;
using MessageBroker.Client.QueueConsumerCoordination;
using MessageBroker.Client.SocketClient;
using MessageBroker.Models;
using MessageBroker.Serialization;

namespace MessageBroker.Client
{
    public class QueueManager
    {
        private readonly ISocketClient _socketClient;
        private readonly IQueueConsumerCoordinator _queueConsumerCoordinator;

        private readonly ISerializer _serializer;
        
        public event Action<QueueConsumerMessage> MessageReceived;

        
        private string _name;
        private string _route;
        
        public string Name => _name;
        public string Route => _route;
        private bool _queueDeclared;
        private bool _queueSubscribed;
        
        public QueueManager(ISerializer serializer, ISocketClient socketClient, IQueueConsumerCoordinator queueConsumerCoordinator)
        {
            _serializer = serializer;
            _socketClient = socketClient;
            _queueConsumerCoordinator = queueConsumerCoordinator;
        }

        public void Setup(string name, string route)
        {
            _route = route;
            _name = name;
            _queueConsumerCoordinator.Add(this);
        }
        
        public async Task<SendAsyncResult> DeclareQueue()
        {
            if (_queueDeclared)
                return SendAsyncResult.AlreadyCompleted;
            
            var sendPayload = QueueDeclareSendPayload();
            
            var result = await _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, true);

            if (result.IsSuccess)
                _queueDeclared = true;

            return result;
        }

        public async Task<SendAsyncResult> DeleteQueue()
        {
            var sendPayload = QueueDeleteSendPayload();
            
            var result = await _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, true);

            if (result.IsSuccess)
            {
                _queueDeclared = false;
                _queueSubscribed = false;
            }

            return result;
        }

        public async Task<SendAsyncResult> SubscribeQueue()
        {
            if (_queueSubscribed)
                return SendAsyncResult.AlreadyCompleted;
    
            var sendPayload = SubscribeSendPayload();
    
            var result = await _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, true);

            if (result.IsSuccess)
                _queueSubscribed = true;

            return result;
        }

        public async Task<SendAsyncResult> UnSubscribeQueue()
        {
            if (!_queueSubscribed)
                return SendAsyncResult.AlreadyCompleted;
            
            var sendPayload = UnSubscribeSendPayload();
    
            var result = await _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, true);

            if (result.IsSuccess)
                _queueSubscribed = false;

            return result;
        }
        
        private SerializedPayload QueueDeclareSendPayload()
        {
            var payload = new QueueDeclare
            {
                Id = Guid.NewGuid(),
                Name = _name,
                Route = Route
            };
    
            return _serializer.Serialize(payload);
        }
        
        private SerializedPayload QueueDeleteSendPayload()
        {
            var payload = new QueueDelete
            {
                Id = Guid.NewGuid(),
                Name = _name
            };
    
            return _serializer.Serialize(payload);
        }
    
        private SerializedPayload SubscribeSendPayload()
        {
            var payload = new SubscribeQueue
            {
                Id = Guid.NewGuid(),
                QueueName = _name
            };
    
            return _serializer.Serialize(payload);
        }
        
        private SerializedPayload UnSubscribeSendPayload()
        {
            var payload = new UnsubscribeQueue
            {
                Id = Guid.NewGuid(),
                QueueName = _name
            };
    
            return _serializer.Serialize(payload);
        }

        internal void OnMessage(QueueMessage queueMessage)
        {
            var queueConsumerMessage = new QueueConsumerMessage
            {
                MessageId = queueMessage.Id,
                Data = queueMessage.Data,
                Route = queueMessage.Route,
                QueueName = queueMessage.QueueName
            };
            
            MessageReceived?.Invoke(queueConsumerMessage);
        }
    }
}