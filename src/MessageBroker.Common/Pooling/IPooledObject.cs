namespace MessageBroker.Common.Pooling
{
    public interface IPooledObject
    {
        bool IsReturnedToPool { get; }

        void SetPooledStatus(bool isReturned);
    }
}