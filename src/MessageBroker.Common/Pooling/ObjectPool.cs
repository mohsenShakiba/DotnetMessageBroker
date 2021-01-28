using System;
using System.Collections.Concurrent;

namespace MessageBroker.Common.Pooling
{
    public class ObjectPool
    {
        
        private static ObjectPool _shared;

        public static ObjectPool Shared
        {
            get
            {
                if (_shared == null)
                    _shared = new ObjectPool();
                return _shared;
            }
        }

        private ConcurrentDictionary<Type, ConcurrentQueue<object>> _d;
        private ConcurrentDictionary<Type, int> _stat;
        private ConcurrentDictionary<Type, int> _created;

        public ObjectPool()
        {
            _d = new();
            _stat = new();
            _created = new();
        }

        public T Rent<T>() where T : new()
        {
            var type = typeof(T);

            if (!_d.ContainsKey(type))
            {
                _d[type] = new ConcurrentQueue<object>();
                _stat[type] = 0;
                _created[type] = 0;
            }

            var bag = _d[type];

            if (bag.TryDequeue(out var o))
            {
                _stat[type] -= 1;
                return (T) o;
            }

            _created[type] += 1;

            return new T();
        }

        public void Return<T>(T o)
        {
            var type = typeof(T);

            _d[type].Enqueue(o);
            _stat[type] += 1;
        }

        public void Dispose()
        {
            Console.WriteLine("test");
        }
    }
}