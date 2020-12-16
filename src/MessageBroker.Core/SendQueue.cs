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
        private int _maxConcurrency;
        private int _currentConcurrency;
        private int _totalSentCount;

        public int CurrentCuncurrency => _currentConcurrency;
        public IReadOnlyList<Guid> PendingMessages => _pendingMessages;
        public IClientSession Session => _session;

        public SendQueue(IClientSession session, int maxConcurrency = 10, int currentConcurrency = 0)
        {
            _maxConcurrency = maxConcurrency;
            _currentConcurrency = currentConcurrency;
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
                if (!IsQueueFull)
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
            _session.SendSync(b);
            _totalSentCount += 1;
        }

        private bool IsQueueFull => _currentConcurrency >= _maxConcurrency;

    }
}
