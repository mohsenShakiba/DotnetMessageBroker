using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageBroker.Core.Configurations;
using MessageBroker.Core.Payloads;
using MessageBroker.Core.Serialize;

namespace MessageBroker.Core.BufferPool
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
        
        private readonly ConcurrentBag<SendPayload> _sendPayloadPool;
        private readonly ConcurrentBag<BinarySerializeHelper> _binarySerializeHelperPool;
        private readonly ConcurrentBag<BinaryDeserializeHelper> _binaryDeserializeHelperPool;
        
        public ObjectPool()
        {
            _sendPayloadPool = new();
            _binarySerializeHelperPool = new();
            _binaryDeserializeHelperPool = new();
        }


        public BinarySerializeHelper RentBinarySerializeHelper()
        {
            if (_binarySerializeHelperPool.TryTake(out var bsh))
            {
                bsh.Refresh();
                return bsh;
            }
            bsh = new BinarySerializeHelper();
            bsh.Setup();
            return bsh;
        }

        public BinaryDeserializeHelper RentDeSerializeBinaryHelper()
        {
            if (_binaryDeserializeHelperPool.TryTake(out var bdh))
            {
                return bdh;
            }
            bdh = new BinaryDeserializeHelper();
            return bdh;
        }

        public SendPayload RentSendPayload()
        {
            if (_sendPayloadPool.TryTake(out var sp))
            {
                return sp;
            }
            sp = new SendPayload();
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
