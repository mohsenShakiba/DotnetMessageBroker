using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Server
{
    public class SessionConfiguration
    {
        public int DefaultHeaderSize { get; set; }
        public int DefaultMaxBodySize { get; set; }

        public static SessionConfiguration Default()
        {
            return new SessionConfiguration
            {
                DefaultHeaderSize = 4,
                DefaultMaxBodySize = 1024
            };
        }
    }
}
