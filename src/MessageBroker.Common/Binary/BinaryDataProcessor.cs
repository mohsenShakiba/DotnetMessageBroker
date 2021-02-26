using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
using MessageBroker.Common.Pooling;
using MessageBroker.Common.Utils;

namespace MessageBroker.Common.Binary
{
    public class BinaryDataProcessor : IBinaryDataProcessor
    {
        private readonly DynamicBuffer _dynamicBuffer;
        private readonly object _lock;
        private readonly Guid _id;
        public bool Debug { get; set; }
        public BinaryDataProcessor()
        {
            _dynamicBuffer = new DynamicBuffer();
            _lock = new object();
            _id = Guid.NewGuid();
            ShowStat();
            
        }

        private void ShowStat()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    
                    using(var sw = File.AppendText(@$"C:\Users\m.shakiba.PSZ021-PC\Desktop\testo\binary_{_id}.txt"))
                    {
                        var length = _dynamicBuffer.Length();
                        sw.WriteLine($"received message in client count is {length}");

                        if (length > 0)
                        {
                            var headerSiz = _dynamicBuffer.Read(BinaryProtocolConfiguration.PayloadHeaderSize);
                            var headerSize = BitConverter.ToInt32(headerSiz.Span);

                            var raminingSizeMatch = length - 4 == headerSize;
                            
                            sw.WriteLine($"dump is {headerSize} from {length} and {raminingSizeMatch}");
                        }
                        
                    }
                }
            });

        }

        public void Write(Memory<byte> chunk)
        {
            lock (_lock)
            {
                _dynamicBuffer.Write(chunk);
            }
        }

        public bool TryRead(out BinaryPayload binaryPayload)
        {
            
            lock (_lock)
            {
                try
                {
                    binaryPayload = null;
                    var canReadHeaderSize = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize);

                    if (!canReadHeaderSize)
                    {
                        if (Debug) 
                            Logger.LogInformation($"cant read header {_dynamicBuffer.Length()}");
                        return false;
                    }

                    var headerSizeBytes = _dynamicBuffer.Read(BinaryProtocolConfiguration.PayloadHeaderSize);
                    var headerSize = BitConverter.ToInt32(headerSizeBytes.Span);

                    var canReadPayload = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

                    if (!canReadPayload)
                    {
                        if (Debug) 
                            Logger.LogInformation($"cant read body {_dynamicBuffer.Length()}");
                        return false;
                    }

                    var payload = _dynamicBuffer.ReadAndClear(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

                    var receiveDataBuffer = ArrayPool<byte>.Shared.Rent(payload.Length);

                    payload.CopyTo(receiveDataBuffer);

                    binaryPayload = ObjectPool.Shared.Rent<BinaryPayload>();
                    binaryPayload.Setup(receiveDataBuffer, payload.Length);

                    return true;
                }
                catch (Exception e)
                {
                   
                    binaryPayload = null;
                    
                    return false;

                }
                
            }
        }
    }
}