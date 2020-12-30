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

        private readonly IBufferPool _bufferPool;
        private readonly ISerializer _serializer;

        public TestMessageConversion()
        {
            _bufferPool = new DefaultBufferPool();
            _serializer = new DefaultSerializer(_bufferPool);
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
            ArrayPool<byte>.Shared.Return(res.OriginalData);
            _bufferPool.ReturnSendPayload(res);
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
