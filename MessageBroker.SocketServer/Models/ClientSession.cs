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
        private readonly Action<MessagePayload> _onMessage;

        public ClientSession(TcpServer server, Action<MessagePayload> onMessage): base(server)
        {
            _onMessage = onMessage;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            var data = buffer.AsMemory((int)offset, (int)size);
            _onMessage?.Invoke(new MessagePayload(data, Id));
        }

    }
}
