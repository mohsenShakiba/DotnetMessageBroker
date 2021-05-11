using System;
using System.Text;
using MessageBroker.Common.Binary;
using Tests.Classes;
using Xunit;

namespace Tests.Common.Binary
{
    public class BinaryDataProcessorTests
    {
        [Fact]
        public void TestBinaryDataProcessorWithDataSplitIntoChunks()
        {
            var size = 1000;
            var numberOfChunks = 10;
            var chunkSize = size / numberOfChunks;

            var randomString = RandomGenerator.GenerateString(size);

            var binaryDataProcessor = new BinaryDataProcessor();
            
            var binaryData = Encoding.UTF8.GetBytes(randomString);

            binaryDataProcessor.Write(BitConverter.GetBytes(size));

            for (var i = 0; i < numberOfChunks; i++)
            {
                var chunk = binaryData.AsMemory(i * chunkSize, chunkSize);
                binaryDataProcessor.Write(chunk);
            }

            // write some random data at the end
            binaryDataProcessor.Write(Encoding.UTF8.GetBytes("meaningless data"));

            binaryDataProcessor.TryRead(out var data);

            Assert.Equal(randomString, Encoding.UTF8.GetString(data.DataWithoutSize.Span));
        }
    }
}