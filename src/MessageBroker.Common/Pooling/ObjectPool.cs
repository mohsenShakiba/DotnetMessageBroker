using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MessageBroker.Common.Pooling
{
    public class ObjectPool
    {
        private static ObjectPool _shared;

        private readonly Dictionary<Type, Queue<object>> _objectTypeDict;
        private readonly ConcurrentDictionary<Type, int> _objectTypeStatDict;

        public ObjectPool()
        {
            _objectTypeDict = new Dictionary<Type, Queue<object>>();
            _objectTypeStatDict = new ConcurrentDictionary<Type, int>();
        }

        public static ObjectPool Shared
        {
            get
            {
                if (_shared == null)
                    _shared = new ObjectPool();
                return _shared;
            }
        }

        public T Rent<T>() where T : IPooledObject, new()
        {
            lock (_objectTypeDict)
            {
                var type = typeof(T);

                if (!_objectTypeDict.ContainsKey(type))
                {
                    _objectTypeDict[type] = new Queue<object>();
#if DEBUG
                _objectTypeStatDict[type] = 0;
#endif
                }

                var bag = _objectTypeDict[type];

                if (bag.TryDequeue(out var o))
                {
                    var i = (T) o;

                    if (!i.IsReturnedToPool)
                    {
                        throw new Exception();
                    }

                    i.SetPooledStatus(false);
                    return i;
                }

                ;

#if DEBUG
            _objectTypeStatDict[type] += 1;
#endif

                return new T();
            }
        }

        public void Return<T>(T o) where T : IPooledObject
        {
            lock (_objectTypeDict)
            {
                var type = typeof(T);

                o.SetPooledStatus(true);

                _objectTypeDict[type].Enqueue(o);
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