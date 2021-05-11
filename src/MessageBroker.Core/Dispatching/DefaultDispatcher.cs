﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MessageBroker.Core.Clients;

namespace MessageBroker.Core.DispatchPolicy
{
    /// <inheritdoc />
    public class DefaultDispatcher : IDispatcher
    {
        private readonly ConcurrentDictionary<Guid, IClient> _clients;
        private readonly ReaderWriterLockSlim _wrLock;


        public DefaultDispatcher()
        {
            _clients = new();
            _wrLock = new();
        }

        public void Add(IClient client)
        {
            try
            {
                _wrLock.EnterWriteLock();

                if (_clients.Keys.Any(sendQueueId => sendQueueId == client.Id))
                {
                    throw new Exception("Added SendQueue already exists");
                }
                
                _clients[client.Id] = client;
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }
        }

        public bool Remove(IClient client)
        {
            try
            {
                _wrLock.EnterWriteLock();

                return _clients.Remove(client.Id, out _);
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }
        }


        public IClient NextAvailable()
        {
            _wrLock.EnterReadLock();
            try
            {
                foreach (var (_, client) in _clients)
                {
                    if (!client.ReachedMaxConcurrency)
                    {
                        return client;
                    }
                }

                return null;
            }
            finally
            {
                _wrLock.ExitReadLock();
            }
     
        }

    }
}