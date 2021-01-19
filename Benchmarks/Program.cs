using BenchmarkDotNet.Running;
using System.Buffers;
using BenchmarkDotNet.Configs;

namespace Benchmarks
{
    class Program
    {

        static void Main(string[] args)
        {

            var summary = BenchmarkRunner.Run(typeof(Program).Assembly, new DebugInProcessConfig());
        }
    }
}
