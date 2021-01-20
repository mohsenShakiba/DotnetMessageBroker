using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Client.SocketClient;
using MessageBroker.Models.Models;
using MessageBroker.Serialization;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Client
{
    public class MessageBrokerClient
    {

        private readonly IPEndPoint _endPoint;
        private readonly ISerializer _serializer;

        private ISocketClient _socketClient;
        private ILoggerFactory _loggerFactory;
        private List<QueueConsumer> _consumers;
        
        public MessageBrokerClient(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
            _serializer = new Serializer();
            _loggerFactory = new LoggerFactory();
            _consumers = new();
        }


        public void Connect(bool retryOnFailure)
        {
            _socketClient.Connect(_endPoint, retryOnFailure);
        }

        public Task PublishAsync(string route, byte[] data)
        {
            var sendPayload = GetMessageData(route, data);
            return _socketClient.SendAsync(sendPayload.Id, sendPayload.Data, false);
        }

        public SendPayload GetMessageData(string route, byte[] data)
        {
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                Data = data,
                Route = route,
            };

            return _serializer.ToSendPayload(msg);
        }

        public void SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        
        public async Task ReceiveMessage(CancellationToken token)
        {
            while (true)
            {
                var payloadData = await _socketClient.ReceiveAsync();
                OnDataReceived(payloadData);
            }
        }

        private void OnDataReceived(Memory<byte> data)
        {
            var payloadType = _serializer.ParsePayloadType(data);
                
            payloadType switch
            {
                PayloadType.Msg => OnMessageReceived(data),
                PayloadType.Ack => expr,
                PayloadType.Nack => expr,
                PayloadType.SubscribeQueue => expr,
                PayloadType.UnSubscribeQueue => expr,
                PayloadType.Register => expr,
                PayloadType.QueueCreate => expr,
                PayloadType.QueueDelete => expr,
                _ => throw new ArgumentOutOfRangeException()
            }
        }

        private void OnMessageReceived(Memory<byte> data)
        {
            var msg = _serializer.ToMessage(data);

            foreach (var consumer in _consumers)
            {
                
            }
        }
    }
}