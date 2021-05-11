using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Common.Logging;
using MessageBroker.Models;
using MessageBroker.TCP.Binary;

namespace Tests.Classes
{
    public class MessageStore
    {
        private readonly ConcurrentDictionary<Guid, Message> _pendingMessage;
        private readonly ConcurrentDictionary<Guid, Message> _messageList;
        private readonly ConcurrentDictionary<Guid, bool> _receivedMessages;
        private readonly ConcurrentDictionary<Guid, bool> _okMessages;
        
        private readonly int _numberOfMessages;
        private readonly string _defaultRoute;
        private int _receivedCount;
        private int _okCount;


        public int CurrentCount => _messageList.Count;
        public int ReceivedCount => _receivedCount;
        public ManualResetEvent ResetEvent { get; }

        public MessageStore(string defaultRoute, int numberOfMessages)
        {
            _defaultRoute = defaultRoute;
            _numberOfMessages = numberOfMessages;

            _messageList = new ConcurrentDictionary<Guid, Message>(10, numberOfMessages);
            ResetEvent = new ManualResetEvent(false);
            _okMessages = new();
            _receivedMessages = new();
            _pendingMessage = new();
        }
        
        public Message GetUniqueMessage(string route = null)
        {
            var id = Guid.NewGuid();
            
            var msg = new Message
            {
                Id = id,
                Data = id.ToByteArray(),
                Route = route ?? _defaultRoute
            };
            
            _pendingMessage[id] = msg;

            return msg;
        }

        public void OnOkReceived(Guid id)
        {
            Logger.LogInformation($"ok recieved pre for message with id {id}");
            if (_pendingMessage.ContainsKey(id))
            {
                Interlocked.Increment(ref _okCount);
                _okMessages[id] = true;
                Logger.LogInformation($"ok recieved for message with id {id}");
            }

            if (_receivedCount == _numberOfMessages)
            {
                ResetEvent.Set();
            }
        }

        public void Commit(Guid id, Message message)
        {
            _messageList[id] = message;
        }

        public void OnMessageReceived(Guid id)
        {
            if (_messageList.ContainsKey(id))
            {
                Interlocked.Increment(ref _receivedCount);
                _receivedMessages[id] = true;
            }

            if (_receivedCount == _numberOfMessages)
            {
                ResetEvent.Set();
            }
        }

        public void WaitForDataToFinish()
        {
            while (true)
            {
                var lastTimeCheck = _receivedCount;
                
                Task.Delay(1000);

                if (_receivedCount > lastTimeCheck)
                {
                    continue;
                }
                else if (_receivedCount < _numberOfMessages)
                {
                    PrintPendingMessages();
                    throw new Exception($"number of received messages is {_receivedCount}");
                }
                else
                {
                    break;
                }
            }
        }
        
        public void WaitForSendDataToFinish()
        {
            while (true)
            {
                var lastTimeCheck = _okCount;
                
                Task.Delay(1000);

                if (_okCount > lastTimeCheck)
                {
                    continue;
                }
                else if (_okCount < _numberOfMessages)
                {
                    PrintNotReceivedMessages();
                    throw new Exception($"number of send messages is {_okCount}");
                }
                else
                {
                    break;
                }
            }
        }
        
        public void PrintPendingMessages()
        {
            foreach (var (key, _) in _messageList)
            {
                if (!_receivedMessages.ContainsKey(key))
                {
                    Logger.LogWarning($"Message {key} was not received");
                }
            }
        }
        
        public void PrintNotReceivedMessages()
        {
            foreach (var (key, _) in _messageList)
            {
                if (!_okMessages.ContainsKey(key))
                {
                    Logger.LogWarning($"Message {key} was not sent");
                }
            }
        }
        
        
    }
}