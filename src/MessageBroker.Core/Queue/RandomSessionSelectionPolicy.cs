using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Queue
{
    public class RandomSessionSelectionPolicy : ISessionSelectionPolicy
    {

        private IHashTable
        public void AddSession(Guid sessionId)
        {
            throw new NotImplementedException();
        }

        public Guid GetNextSession(string route)
        {
            throw new NotImplementedException();
        }

        public void RemoveSession(Guid sessionId)
        {
            throw new NotImplementedException();
        }
    }
}
