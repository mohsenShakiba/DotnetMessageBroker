using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MessageBroker.Core.BufferPool;
using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using System;
using System.Buffers;

namespace Benchmarks
{

    [MemoryDiagnoser]
    public class TestMessageConversion
    {

        private readonly ISerializer _serializer;

        public TestMessageConversion()
        {
            var bufferPool = new DefaultBufferPool();
            _serializer = new DefaultSerializer(bufferPool);
        }

        [Benchmark]
        public void TestCreateAck()
        {
            var ack = new Ack { Id = Guid.NewGuid() };
            var res = _serializer.ToSendPayload(ack);
        }

        [Benchmark]
        public void TestCreateAckUsingDispose()
        {
            var ack = new Ack { Id = Guid.NewGuid() };
            var res = _serializer.ToSendPayload(ack);
            res.Dispose();
        }

    }

    class Program
    {

        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
