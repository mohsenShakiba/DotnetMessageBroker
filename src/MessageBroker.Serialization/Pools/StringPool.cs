using System;
using System.Collections.Generic;
using System.Text;

namespace MessageBroker.Serialization.Pools
{
    public class StringPool
    {
        private static StringPool _shared;
        private readonly Dictionary<int, byte[]> _reverseStore = new();

        private readonly Dictionary<int, string> _store = new();

        public static StringPool Shared
        {
            get
            {
                if (_shared == null)
                    _shared = new StringPool();
                return _shared;
            }
        }

        public string GetStringForBytes(Span<byte> data)
        {
            var hashCode = ComputeHash(data);

            if (_store.TryGetValue(hashCode, out var s)) return s;

            s = Encoding.UTF8.GetString(data);

            _store[hashCode] = s;

            return s;
        }

        public void TryCopyTo(string str, Span<byte> buffer)
        {
            var hash = str.GetHashCode();
            if (_reverseStore.TryGetValue(hash, out var b))
            {
                b.CopyTo(buffer);
            }
            else
            {
                b = Encoding.UTF8.GetBytes(str);
                _reverseStore[hash] = b;
                b.CopyTo(buffer);
            }
        }

        private int ComputeHash(Span<byte> data)
        {
            unchecked
            {
                const int p = 16777619;
                var hash = (int) 2166136261;

                for (var i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
    }
}