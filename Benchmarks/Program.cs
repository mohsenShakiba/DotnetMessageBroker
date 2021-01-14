using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MessageBroker.Core.BufferPool;
using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using System;
using System.Buffers;
using System.Text;

namespace Benchmarks
{

    [MemoryDiagnoser]
    public class TestMessageConversion
    {

        private readonly IBufferPool _bufferPool;
        private readonly ISerializer _serializer;
        private readonly Memory<byte> _strBytes;

        public TestMessageConversion()
        {
            _bufferPool = new DefaultBufferPool();
            _serializer = new DefaultSerializer(_bufferPool);
            _strBytes = Encoding.UTF8.GetBytes("this is a long text");
        }

        [Benchmark]
        public void TestStringInterning()
        {
            var s1 = Encoding.UTF8.GetString(_strBytes.Span);
            string.Intern(s1);
            var s2 = Encoding.UTF8.GetString(_strBytes.Span);
            var isInterened = string.IsInterned(s2);

            if (s1.Equals(s2))
            {
                Object.ReferenceEquals(s1, s2);
                Console.WriteLine("true");
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
            var t = new TestMessageConversion();
            t.TestStringInterning();

            //var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
