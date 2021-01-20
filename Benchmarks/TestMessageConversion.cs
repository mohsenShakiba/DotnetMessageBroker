using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using MessageBroker.Core;
using MessageBroker.Core.Persistance;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Models.Models;
using MessageBroker.Serialization;
using MessageBroker.SocketServer;
using Microsoft.Extensions.Logging;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class TestMessageConversion
    {
        private readonly Coordinator _coordinator;
        private readonly InMemoryMessageStore _imessageStore;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly RouteMatcher _routeMatcher;
        private readonly SendPayload _sendPayload;

        private readonly ISerializer _serializer;
        private readonly SessionResolver _sessionResolver;

        public TestMessageConversion()
        {
            _serializer = new Serializer();
            _sessionResolver = new SessionResolver();
            _messageDispatcher = new MessageDispatcher(_sessionResolver, _serializer);

            var loggerFactory = LoggerFactory.Create(builder => { });

            _routeMatcher = new RouteMatcher();
            _imessageStore = new InMemoryMessageStore();

            _coordinator = new Coordinator(_sessionResolver, _serializer, _messageDispatcher, _routeMatcher,
                _imessageStore, loggerFactory.CreateLogger<Coordinator>());

            var sessionId = Guid.NewGuid();
            var testSession = new TestClientSession();
            testSession.SessionId = sessionId;

            _sessionResolver.Add(testSession);

            // add session to coordinator
            var subPayload = new Register
            {
                Concurrency = 10,
                Id = Guid.NewGuid()
            };

            var subscribeSendData = _serializer.ToSendPayload(subPayload);
            _coordinator.DataReceived(sessionId, subscribeSendData.DataWithoutSize);

            // create queue 
            var queuePayload = new QueueDeclare
            {
                Id = Guid.NewGuid(),
                Name = "TEST",
                Route = "TEST"
            };
            var queueDeclareSendData = _serializer.ToSendPayload(queuePayload);
            _coordinator.DataReceived(sessionId, queueDeclareSendData.DataWithoutSize);

            // listen to queue
            var listenPayload = new SubscribeQueue
            {
                Id = Guid.NewGuid(),
                QueueName = "TEST"
            };
            var listenSendPayload = _serializer.ToSendPayload(listenPayload);

            _coordinator.DataReceived(sessionId, listenSendPayload.DataWithoutSize);

            var message = new Message
                {Id = Guid.NewGuid(), Route = "TEST", Data = Encoding.UTF8.GetBytes("SAMPLE TEST DATA")};
            _sendPayload = _serializer.ToSendPayload(message);
        }

        [Benchmark]
        public void TestCreateAck()
        {
            var sessionId = Guid.NewGuid();

            for (var i = 0; i < 10000; i++)
            {
                var msg = _serializer.ToMessage(_sendPayload.DataWithoutSize);
                //_bufferPool.Return(msg.OriginalMessageData);
                _coordinator.OnMessage(sessionId, msg);
            }
        }

        //[Benchmark]
        //public void TestCreateAck()
        //{
        //    var ack = new Ack { Id = Guid.NewGuid() };
        //    var res = _serializer.ToSendPayload(ack);
        //}

        //[Benchmark]
        //public void TestCreateAckUsingDispose()
        //{
        //    var ack = new Ack { Id = Guid.NewGuid() };
        //    var res = _serializer.ToSendPayload(ack);
        //    _bufferPool.ReturnSendPayload(res);
        //}
    }
}