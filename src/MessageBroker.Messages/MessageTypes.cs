using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Messages
{
    public class MessageTypes
    {
        public const string Ack = "ACK";
        public const string Nack = "NACK";
        public const string Message = "MSG";
        public const string RegisterPublisher = "REGPUB";
        public const string RegisterSubscriber = "REGSUB";
        public const string UnRegister = "UREG";
        public const string ListenRoute = "LROUT";
        public const string UnlistenListenRoute = "ULROUT";
    }
}
