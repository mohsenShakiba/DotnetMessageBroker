using System;
using System.Collections.Concurrent;

namespace MessageBroker.Common.Pooling
{
    public class ObjectPool
    {
        private static ObjectPool _shared;

        private readonly ConcurrentDictionary<Type, ConcurrentQueue<object>> _objectTypeDict;
        private readonly ConcurrentDictionary<Type, int> _objectTypeStatDict;

        public ObjectPool()
        {
            _objectTypeDict = new ConcurrentDictionary<Type, ConcurrentQueue<object>>();
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

        public T Rent<T>() where T : new()
        {
            var type = typeof(T);

            if (!_objectTypeDict.ContainsKey(type))
            {
                _objectTypeDict[type] = new ConcurrentQueue<object>();

#if DEBUG
                _objectTypeStatDict[type] = 0;
#endif
            }

            var bag = _objectTypeDict[type];

            if (bag.TryDequeue(out var o)) return (T) o;

#if DEBUG
            _objectTypeStatDict[type] += 1;
#endif

            return new T();
        }

        public void Return<T>(T o)
        {
            var type = typeof(T);

            _objectTypeDict[type].Enqueue(o);
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