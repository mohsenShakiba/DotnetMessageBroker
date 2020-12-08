using MessageBroker.Messages;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Models
{
    public class ClientSession: TcpSession
    {
        private readonly Action<Payload> _onMessage;

        public ClientSession(TcpServer server, Action<Payload> onMessage): base(server)
        {
            _onMessage = onMessage;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            var data = buffer.AsMemory((int)offset, (int)size);
            _onMessage?.Invoke(new Payload(Id, data));
        }

    }
}
