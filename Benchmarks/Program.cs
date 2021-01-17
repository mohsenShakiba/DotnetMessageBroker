using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MessageBroker.Core;
using MessageBroker.Core.BufferPool;
using MessageBroker.Core.MessageRefStore;
using MessageBroker.Core.Models;
using MessageBroker.Core.Persistance;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.Serialize;
using MessageBroker.SocketServer;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Text;
using BenchmarkDotNet.Configs;
using MessageBroker.Core.Payloads;
using MessageBroker.SocketServer.Abstractions;

namespace Benchmarks
{

    [MemoryDiagnoser]
    public class TestMessageConversion
    {

        private readonly ISerializer _serializer;
        private readonly SessionResolver _sessionResolver;
        private readonly DefaultMessageRefStore _messageRefStore;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly DefaultRouteMatching _defaultRouteMatcher;
        private readonly InMemoryMessageStore _imessageStore;
        private readonly Coordinator _coordinator;
        private readonly SendPayload _sendPayload;

        public TestMessageConversion()
        {
            _serializer = new DefaultSerializer();
            _sessionResolver = new SessionResolver();
            _messageRefStore = new DefaultMessageRefStore();
            _messageDispatcher = new MessageDispatcher(_sessionResolver, _serializer, _messageRefStore);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
            });

            _defaultRouteMatcher = new DefaultRouteMatching();
            _imessageStore = new InMemoryMessageStore();
            
            _coordinator = new Coordinator(_sessionResolver, _serializer, _messageDispatcher, _defaultRouteMatcher, _imessageStore, _messageRefStore, loggerFactory.CreateLogger<Coordinator>());

            var sessionId = Guid.NewGuid();
            var testSession = new TestClientSession();
            testSession.SessionId = sessionId;
            
            _sessionResolver.Add(testSession);
            
            // add session to coordinator
            var subPayload = new Subscribe
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

            var message = new Message { Id = Guid.NewGuid(), Route = "TEST", Data = Encoding.UTF8.GetBytes("SAMPLE TEST DATA") };
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

    class Program
    {

        static void Main(string[] args)
        {

            var summary = BenchmarkRunner.Run(typeof(Program).Assembly, new DebugInProcessConfig());
        }
    }

    public class TestClientSession : IClientSession
    {
        public Guid SessionId { get; set; }
        
        public void SetupSendCompletedHandler(Action onSendCompleted)
        {
        }

        public void Send(Memory<byte> payload)
        {
            // do nothing
        }

        public bool SendAsync(Memory<byte> payload)
        {
            return false;
        }

        public void Close()
        {
            // do nothing
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}
