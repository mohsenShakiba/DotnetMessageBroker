using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using BenchmarkDotNet.Attributes;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using MessageBroker.Core;
using MessageBroker.Core.InternalEventChannel;
using MessageBroker.Core.MessageIdTracking;
using MessageBroker.Core.Persistence.InMemoryStore;
using MessageBroker.Core.Queues;
using MessageBroker.Core.RouteMatching;
using MessageBroker.Core.StatRecording;
using MessageBroker.Models;
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
        private readonly IEventChannel _eventChannel;
        private readonly IStatRecorder _statRecorder;
        private readonly MessageIdTracker _messageIdTracker;
        private readonly IQueueStore _queueStore;
        private readonly Channel<int> _testChan;

        public TestMessageConversion()
        {
            _serializer = new Serializer();
            _sessionResolver = new SessionResolver();
            _eventChannel = new EventChannel();
            _messageDispatcher = new MessageDispatcher(_sessionResolver, _eventChannel);

            var loggerFactory = LoggerFactory.Create(builder => { });

            _routeMatcher = new RouteMatcher();
            _imessageStore = new InMemoryMessageStore();
            _statRecorder = new StatRecorder();
            _messageIdTracker = new MessageIdTracker();
            _queueStore = new QueueStore(null);
            
            _coordinator = new Coordinator(_serializer, _messageDispatcher,
                _messageIdTracker, _queueStore, _statRecorder);

            var sessionId = Guid.NewGuid();
            var testSession = new TestClientSession();
            testSession.SessionId = sessionId;

            // _sessionResolver.Add(testSession);
            //
            // // add session to coordinator
            // var subPayload = new ConfigureSubscription
            // {
            //     Concurrency = 10,
            //     Id = Guid.NewGuid()
            // };
            //
            // var subscribeSendData = _serializer.ToSendPayload(subPayload);
            // _coordinator.DataReceived(sessionId, subscribeSendData.DataWithoutSize);
            //
            // // create queue 
            // var queuePayload = new QueueDeclare
            // {
            //     Id = Guid.NewGuid(),
            //     Name = "TEST",
            //     Route = "TEST"
            // };
            // var queueDeclareSendData = _serializer.ToSendPayload(queuePayload);
            // _coordinator.DataReceived(sessionId, queueDeclareSendData.DataWithoutSize);
            //
            // // listen to queue
            // var listenPayload = new SubscribeQueue
            // {
            //     Id = Guid.NewGuid(),
            //     QueueName = "TEST"
            // };
            // var listenSendPayload = _serializer.ToSendPayload(listenPayload);
            //
            // _coordinator.DataReceived(sessionId, listenSendPayload.DataWithoutSize);
            //
            // var message = new Message
            //     {Id = Guid.NewGuid(), Route = "TEST", Data = Encoding.UTF8.GetBytes("SAMPLE TEST DATA")};
            // _sendPayload = _serializer.ToSendPayload(message);

            _testChan = Channel.CreateUnbounded<int>();
            
            Logger.AddConsole();
        }

        // [Benchmark]
        // public void TestCreateAck()
        // {
        //     var sessionId = Guid.NewGuid();
        //
        //     for (var i = 0; i < 10000; i++)
        //     {
        //         var msg = _serializer.ToMessage(_sendPayload.DataWithoutSize);
        //         //_bufferPool.Return(msg.OriginalMessageData);
        //         _coordinator.OnMessage(sessionId, msg);
        //     }
        //     
        //     
        // }

        [Benchmark]
        public void TestLog()
        {
            for (var i = 0; i < 10; i++)
            {
                var item = ObjectPool.Shared.Rent<SendPayload>();
                ObjectPool.Shared.Return(item);
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

    // public class TestValueTaskSource<T> : IValueTaskSource<T>
    // {
    //
    //     private bool faulted;
    //     private bool resultReady;
    //     private bool cancelled;
    //     private Action<object> continuation;
    //     
    //     public T GetResult(short token)
    //     {
    //     }
    //
    //     public ValueTaskSourceStatus GetStatus(short token)
    //     {
    //         if (cancelled)
    //             return ValueTaskSourceStatus.Canceled;
    //         
    //         if (faulted)
    //             return ValueTaskSourceStatus.Faulted;
    //
    //         if (resultReady)
    //             return ValueTaskSourceStatus.Succeeded;
    //
    //         return ValueTaskSourceStatus.Pending;
    //     }
    //
    //     public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
    //     {
    //         Interlocked.CompareExchange(ref this.continuation, continuation, null);
    //     }
    //     
    // }
}