using MessageBroker.Messages;
using MessageBroker.SocketServer.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Core
{
    public class SendQueue
    {
        private readonly IClientSession _session;
        private readonly Parser _parser;
        private ConcurrentQueue<Message> _queue;
        private readonly List<Guid> _pendingMessages;
        private int _maxConcurrency = 10;
        private int _currentConcurrency = 0;

        public int CurrentCuncurrency => _currentConcurrency;
        public IReadOnlyList<Guid> PendingMessages => _pendingMessages;

        public SendQueue(IClientSession session)
        {
            _session = session;
            _queue = new();
            _pendingMessages = new();
            _parser = new();
        }

        public void Enqueue(Message msg)
        {
            if (IsQueueFull)
            {
                _queue.Enqueue(msg);
            }else
            {
                Send(msg);
            }
        }

        public void ReleaseOne(Ack ack)
        {
            if (_pendingMessages.Contains(ack.MsgId))
            {
                Interlocked.Decrement(ref _currentConcurrency);
                SendPendingMessages();
            }
        }

        private void SendPendingMessages()
        {
            if (_queue.TryDequeue(out var msg))
                Send(msg);
        }
        
        private void Send(Message msg)
        {
            Interlocked.Increment(ref this._currentConcurrency);
            _pendingMessages.Add(msg.Id);
            var b = _parser.ToBinary(msg);
            _session.Send(b);
        }

        private bool IsQueueFull => _currentConcurrency >= _maxConcurrency;

    }
}
