using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Models;
using Microsoft.Extensions.Logging;

namespace Tests.Classes
{
    public class MessageStore
    {
        
        private int _numberOfMessages;
        private string _defaultRoute;
        
        private readonly ConcurrentDictionary<Guid, Message> _allMessages;
        
        private readonly ConcurrentDictionary<Guid, bool> _sentMessages;
        private readonly ConcurrentDictionary<Guid, bool> _receivedMessages;
        private readonly ILogger<MessageStore> _logger;

        public int ReceivedCount => _receivedMessages.Count;
        public int SentCount => _sentMessages.Count;


        public MessageStore(ILogger<MessageStore> logger)
        {
            _logger = logger;

            _allMessages = new();
            _receivedMessages = new();
            _sentMessages = new();
        }

        public void Setup(string defaultRoute, int numberOfMessages)
        {
            _defaultRoute = defaultRoute;
            _numberOfMessages = numberOfMessages;
        }
        
        public Message NewMessage(string route = null)
        {
            var id = Guid.NewGuid();
            
            var msg = new Message
            {
                Id = id,
                Data = id.ToByteArray(),
                Route = route ?? _defaultRoute
            };
            
            _allMessages[id] = msg;

            return msg;
        }

        public void OnMessageSent(Guid id)
        {
            // check if message id is valid
            if (_allMessages.ContainsKey(id))
            {
                _sentMessages[id] = true;
            }
            else
            {
                throw new Exception("Invalid message is was provided");
            }
        }

        public void OnMessageReceived(Guid id)
        {
            if (_allMessages.ContainsKey(id))
            {
                _receivedMessages[id] = true;
            }
            else
            {
                throw new Exception("Invalid message is was provided");
            }
        }

        public void WaitForAllMessageToBeReceived()
        {
            var lastTimeCheck = -1;
            while (true)
            {
                
                // if everything is ok
                if (ReceivedCount == _numberOfMessages)
                {
                    break;
                }
                
                Thread.Sleep(10000);

                // if the number of received count hasn't changed then print the messages
                if (ReceivedCount <= lastTimeCheck)
                {
                    foreach (var (key, _) in _allMessages)
                    {
                        if (!_receivedMessages.ContainsKey(key))
                        {
                            _logger.LogWarning($"Message {key} was not received by the subscription");
                        }
                    }

                    throw new Exception($"Number of received messages is {ReceivedCount} but should be {_numberOfMessages}");
                }

                lastTimeCheck = ReceivedCount;
            }
        }
        
        public void WaitForAllMessageToBeSent()
        {
            var lastTimeCheck = 0;
            while (true)
            {
                
                // if everything is ok
                if (SentCount == _numberOfMessages)
                {
                    break;
                }

                Thread.Sleep(1000);

                // if the number of sent count hasn't changed then print the messages
                if (SentCount <= lastTimeCheck)
                {
                    foreach (var (key, _) in _allMessages)
                    {
                        if (!_sentMessages.ContainsKey(key))
                        {
                            _logger.LogWarning($"Message {key} was not received by the subscription");
                        }
                    }
                    
                    throw new Exception($"Number of received messages is {SentCount} but should be {_numberOfMessages}");
                }

                lastTimeCheck = SentCount;

            }
        }
        
    }
}