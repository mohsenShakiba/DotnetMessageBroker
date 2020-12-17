using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Messages
{
    public class ModelTypes
    {
        public const string Ack = "ACK";
        public const string Nack = "NACK";
        public const string Message = "MSG";
        public const string Listen = "LROUT";
        public const string Unlisten = "ULROUT";
        public const string Subscribe = "SUB";
    }
}
