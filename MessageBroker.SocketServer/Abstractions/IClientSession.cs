using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.SocketServer.Abstractions
{
    public interface IClientSession
    {

        Guid SessionId { get; }
        void SetupSendCompletedHandler(Action onSendCompleted);
        void Send(Memory<byte> payload);
        bool SendAsync(Memory<byte> payload);
        void Close();
        void Dispose();
    }
}
