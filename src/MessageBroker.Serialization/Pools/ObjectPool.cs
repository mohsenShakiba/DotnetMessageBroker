using System.Collections.Concurrent;

namespace MessageBroker.Serialization.Pools
{
    public class ObjectPool
    {
        private static ObjectPool _shared;
        private readonly ConcurrentBag<BinaryDeserializeHelper> _binaryDeserializeHelperPool;
        private readonly ConcurrentBag<BinarySerializeHelper> _binarySerializeHelperPool;

        private readonly ConcurrentBag<SendPayload> _sendPayloadPool;

        public ObjectPool()
        {
            _sendPayloadPool = new ConcurrentBag<SendPayload>();
            _binarySerializeHelperPool = new ConcurrentBag<BinarySerializeHelper>();
            _binaryDeserializeHelperPool = new ConcurrentBag<BinaryDeserializeHelper>();
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


        public BinarySerializeHelper RentBinarySerializeHelper()
        {
            if (_binarySerializeHelperPool.TryTake(out var bsh))
            {
                bsh.Refresh();
                return bsh;
            }

            bsh = new BinarySerializeHelper(SerializationConfig.Default);
            bsh.Setup();
            return bsh;
        }

        public BinaryDeserializeHelper RentDeSerializeBinaryHelper()
        {
            if (_binaryDeserializeHelperPool.TryTake(out var bdh)) return bdh;
            bdh = new BinaryDeserializeHelper();
            return bdh;
        }

        public SendPayload RentSendPayload()
        {
            if (_sendPayloadPool.TryTake(out var sp)) return sp;
            sp = new SendPayload(SerializationConfig.Default);
            return sp;
        }

        public void Return(BinarySerializeHelper bsh)
        {
            _binarySerializeHelperPool.Add(bsh);
        }

        public void Return(BinaryDeserializeHelper bdh)
        {
            _binaryDeserializeHelperPool.Add(bdh);
        }

        public void Return(SendPayload payload)
        {
            _sendPayloadPool.Add(payload);
        }
    }
}