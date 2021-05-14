using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MessageBroker.Common.Logging;

namespace MessageBroker.Common.Pooling
{
    public class ObjectPool
    {
        public static readonly ObjectPool Shared = new();

        private readonly Dictionary<int, Queue<object>> _objectTypeDict;
        private readonly Dictionary<Guid, bool> _pooledObjectDict;
        private readonly ConcurrentDictionary<Type, int> _objectTypeStatDict;

        public ObjectPool()
        {
            _objectTypeDict = new ();
            _pooledObjectDict = new();
            _objectTypeStatDict = new ();
        }

        public T Rent<T>() where T : IPooledObject, new()
        {
            var type = typeof(T);
            var typeKey = type.Name.GetHashCode();
            
            lock (_objectTypeDict)
            {

                if (!_objectTypeDict.ContainsKey(typeKey))
                {
                    _objectTypeDict[typeKey] = new Queue<object>();
#if DEBUG
                    _objectTypeStatDict[type] = 0;
#endif
                }

                var bag = _objectTypeDict[typeKey];

                if (bag.TryDequeue(out var o))
                {
                    var i = (T) o;
#if DEBUG
                    _pooledObjectDict[i.PoolId] = false;
#endif
                    return i;
                }

                var newInstance = new T {PoolId = Guid.NewGuid()};

#if DEBUG
                _pooledObjectDict[newInstance.PoolId] = false;
                _objectTypeStatDict[type] += 1;
#endif

                return newInstance;
            }
        }

        public void Return<T>(T o) where T : IPooledObject
        {
            var type = typeof(T);
            var typeKey = type.Name.GetHashCode();
            
            lock (_objectTypeDict)
            {

#if DEBUG
                var keyExists = _pooledObjectDict.TryGetValue(o.PoolId, out var isReturnedToPool);

                if (!keyExists)
                {
                    throw new InvalidOperationException($"The object with key: {o.PoolId} doesn't belong to {nameof(ObjectPool)}");
                }
                
                if (isReturnedToPool)
                {
                    throw new InvalidOperationException($"The object with key: {o.PoolId} has already been returned to {nameof(ObjectPool)}");
                }
                
                _pooledObjectDict[o.PoolId] = true;

#endif


                _objectTypeDict[typeKey].Enqueue(o);
            }
        }

#if DEBUG
        public int CreatedCount<T>()
        {
            var type = typeof(T);
            return _objectTypeStatDict[type];
        }
#endif
    }
}