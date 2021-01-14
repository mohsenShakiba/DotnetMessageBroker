using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Core.Queue
{
    public class RandomSessionSelectionPolicy : ISessionSelectionPolicy
    {

        private readonly List<Guid> _hashTable;
        private readonly Random _random;
        private readonly ReaderWriterLockSlim _wrLock;

        public RandomSessionSelectionPolicy()
        {
            _hashTable = new();
            _random = new();
            _wrLock = new ReaderWriterLockSlim();
        }

        public void AddSession(Guid sessionId)
        {
            try
            {
                _wrLock.EnterWriteLock();
                _hashTable.Add(sessionId);
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }

        }

        public Guid? GetNextSession()
        {
            try
            {
                _wrLock.EnterReadLock();
                if (_hashTable.Count == 0)
                    return null;

                var randomIndex = _random.Next(0, _hashTable.Count());
                var guid = _hashTable[randomIndex];
                return guid;
            }
            finally
            {
                _wrLock.ExitReadLock();
            }
        }

        public void RemoveSession(Guid sessionId)
        {
            try
            {
                _wrLock.EnterWriteLock();
                _hashTable.Remove(sessionId);
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }
        }
    }
}
