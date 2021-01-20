using System;
using System.Threading.Tasks;

namespace MessageBroker.Client.TaskManager
{
    public interface ITaskManager
    {
        Task<bool> Setup(Guid id, bool completeOnAcknowledge);
    }
}