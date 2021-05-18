using System;

namespace MessageBroker.Common.Pooling
{
    /// <summary>
    /// Marker interface for objects used in pooling
    /// </summary>
    public interface IPooledObject
    {
        public Guid PoolId { get; set; }
    }
}