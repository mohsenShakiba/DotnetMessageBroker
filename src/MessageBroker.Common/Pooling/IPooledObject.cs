using System;

namespace MessageBroker.Common.Pooling
{
    public interface IPooledObject
    {
        public Guid PoolId { get; }
        bool IsReturnedToPool { get; }

        void SetPooledStatus(bool isReturned);
    }

}