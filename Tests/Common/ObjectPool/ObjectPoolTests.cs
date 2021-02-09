using MessageBroker.Serialization;
using Xunit;

namespace Tests.Common.ObjectPool
{
    public class ObjectPoolTests
    {
        
        [Fact]
        public void TestReuseObjects()
        {
            var objectPool = new MessageBroker.Common.Pooling.ObjectPool();

            var rentedObject = objectPool.Rent<SerializedPayload>();
            objectPool.Return(rentedObject);

            _ = objectPool.Rent<SerializedPayload>();
            
            Assert.Equal(1, objectPool.CreatedCount<SerializedPayload>());
        }
    }
}