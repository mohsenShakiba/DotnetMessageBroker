using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MessageBroker.Common.Logging;

namespace MessageBroker.Core.SessionPolicy
{
    public class RoundRobinSessionPolicy : ISessionPolicy
    {
        private readonly List<Guid> _sessions;
        private readonly ReaderWriterLockSlim _wrLock;

        private int _currentIndex;

        public RoundRobinSessionPolicy()
        {
            _sessions = new List<Guid>();
            _wrLock = new ReaderWriterLockSlim();
        }

        public void AddSession(Guid sessionId)
        {
            try
            {
                Logger.LogInformation($"SessionPolicy -> Added session {sessionId}");
                _wrLock.EnterWriteLock();
                
                if (_sessions.Contains(sessionId))
                    throw new Exception("The session already exists");
                
                _sessions.Add(sessionId);
                Logger.LogInformation($"SessionPolicy -> Added session completed {sessionId}");
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }
        }

        public bool HasSession()
        {
            try
            {
                _wrLock.EnterReadLock();

                return _sessions.Any();
            }
            finally
            {
                _wrLock.ExitReadLock();
            } 
        }

        public Guid? GetNextSession()
        {
            try
            {
                _wrLock.EnterReadLock();
                if (_sessions.Count == 0)
                    return null;

                if (_currentIndex >= _sessions.Count)
                {
                    _currentIndex = 0;
                }
                
                var guid = _sessions[_currentIndex++];
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
                Logger.LogInformation($"SessionPolicy -> Removed session completed {sessionId}");
                _sessions.Remove(sessionId);
            }
            finally
            {
                _wrLock.ExitWriteLock();
            }
        }
    }
}