using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Core.SessionPolicy
{
    public interface ISessionPolicy: IDisposable
    {
        void AddSendQueue(ISendQueue sendQueue);
        void RemoveSendQueue(Guid sendQueueId);
        Task<ISendQueue> GetNextAvailableSendQueueAsync(CancellationToken cancellationToken);
    }
}